# Diseño — M0 Foundation

## Objetivo y decisiones

Implementar los siete entregables del [proposal](proposal.md), respetando la [constitución](../CONSTITUTION.md). Se crea una base ejecutable con una comprobación de persistencia real y una pantalla móvil mínima. No se incorporan entidades de jugadores, clubes ni partidos.

## Stack fijado

| Componente | Elección |
| --- | --- |
| SDK .NET | 10.0.400, con actualizaciones de parche compatibles mediante `global.json` |
| Backend | ASP.NET Core 10, EF Core / herramientas 10.0.11 |
| Proveedor PostgreSQL | Npgsql EF Core 10.0.3 |
| Base de datos | PostgreSQL 18.6 |
| Cliente | Expo SDK 57, React Native 0.86.3, React 19.2.3 |
| Herramientas móvil | Node 24 LTS, npm, TypeScript 6; dependencias fijadas por `package-lock.json` |
| Verificación backend | xUnit y `WebApplicationFactory`, con PostgreSQL real |

Versiones verificadas en registros oficiales y en la [matriz de Expo](https://docs.expo.dev/versions/latest/). Se usa la [generación OpenAPI integrada de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) y el [proveedor Npgsql 10](https://www.npgsql.org/efcore/release-notes/10.0.html).

## Organización y dependencias

La solución `PadelMatch.slnx` reúne los cuatro proyectos y las pruebas bajo `backend/`.

```text
Api ──────────> Application ──> Domain
 └────────────> Infrastructure ──> Application
mobile/ (Expo, independiente de la solución .NET)
```

- `Domain`: biblioteca sin dependencias externas ni entidades anticipadas.
- `Application`: contrato mínimo para consultar disponibilidad de persistencia.
- `Infrastructure`: contexto EF Core, migraciones y comprobación PostgreSQL.
- `Api`: composición de dependencias, endpoints técnicos y documento OpenAPI.
- `PadelMatch.Api.Tests`: pruebas de integración; no forma parte del dominio productivo.

No se añaden repositorios genéricos, bus de mensajes, autenticación, navegación final ni abstracciones de casos de uso todavía inexistentes.

## Persistencia y configuración

`PadelMatchDbContext` parte de un modelo vacío. Una migración `InitialFoundation` sin tablas de negocio establece la historia de migraciones en PostgreSQL. Esto permite comprobar inicialización y repetición sin inventar una entidad desechable.

Las migraciones se aplican explícitamente con la herramienta local `dotnet-ef`. La API no modifica el esquema al arrancar. El comando se ejecuta desde la raíz indicando Infrastructure como proyecto de migraciones y Api como proyecto de inicio.

`compose.yaml` define PostgreSQL 18.6, volumen persistente, comprobación de salud y puerto local. Credenciales de desarrollo claramente identificadas en `.env.example`; `.env` queda ignorado. La API acepta `ConnectionStrings__PadelMatch` y cuenta con una configuración exclusivamente local en `appsettings.Development.json`. Fuera de Development se exige proporcionar la conexión.

En este equipo no existe Docker ni PostgreSQL. Para verificar M0 se usarán los [binarios portables de Windows enlazados por PostgreSQL](https://www.postgresql.org/download/windows/), con datos bajo `.local/` y escucha en loopback. No se instala un servicio del sistema. Docker Compose permanece como alternativa documentada para otros entornos.

## API y OpenAPI

- `GET /health/live`: devuelve estado del proceso y un instante UTC del servidor; no depende de la base de datos.
- `GET /health/ready`: devuelve 200 cuando PostgreSQL es accesible y las migraciones están aplicadas; 503 en caso contrario. No expone credenciales ni detalles de excepciones.
- `GET /openapi/v1.json`: contrato de los endpoints disponibles, habilitado en Development. El documento es consultable directamente desde navegador o cliente HTTP; no se requiere una UI Swagger adicional para M0.

La comprobación de base de datos usa EF Core real. Los endpoints tienen contrato tipado y respuestas documentadas. No se implementan las rutas orientativas de negocio del plan.

## Tiempo

Se registra `TimeProvider.System` para obtener instantes UTC del backend y poder sustituir el reloj en pruebas. Los contratos utilizan `DateTimeOffset` en UTC. Los futuros campos persistidos de instante usarán PostgreSQL `timestamp with time zone` y valores UTC; no se crean ahora columnas de partidos.

Se adopta `Europe/Madrid` como zona de presentación compartida del MVP, conforme al contexto local del proyecto. El cálculo de plazos usa duración real entre instantes UTC. No se implementan expiraciones ni políticas del club en Foundation.

## Cliente móvil

Proyecto Expo con TypeScript estricto, registro de la aplicación y una pantalla estática con el nombre del producto y una breve descripción. No se muestran botones que simulen funcionalidades pendientes ni detalles internos del backend en la experiencia de usuario.

Scripts para Metro, Android, iOS, web, comprobación TypeScript y exportación. Web permite comprobar visualmente la pantalla en este entorno Windows sin emuladores. La exportación de bundles Android/iOS valida compilación JavaScript, pero no sustituye ejecutar la app en un dispositivo.

M0 no requiere conectar una funcionalidad de negocio móvil a la API. Se documenta cómo arrancar Expo Go en dispositivo físico y las restricciones de iOS Simulator en Windows.

## Verificación

Las pruebas backend crean una base de datos temporal de nombre aleatorio, aplican la migración dos veces y comprueban que solo queda una entrada de historia. Verifican `/health/ready` antes y después de migrar, el comportamiento con base inexistente, `/health/live` independiente de la base y el contrato OpenAPI. Solo eliminan su propia base temporal al finalizar.

Se comprobarán además restauración y compilación Release, generación de migraciones sin cambios pendientes, TypeScript, compatibilidad Expo y exportación Android/iOS/web. Se registrarán por separado ejecución web y ausencia de ejecución nativa si no hay dispositivos disponibles.

Los resultados y limitaciones se documentarán siguiendo `pastiche-rdd` al terminar las tareas. No se declarará una comprobación nativa como realizada basándose solo en un bundle.

## Alternativas y límites

- SQLite o un mock no demuestran integración PostgreSQL; las pruebas usan la base real.
- La migración inicial vacía evita anticipar el modelo de M1–M13.
- OpenAPI integrado cubre el contrato sin introducir una segunda biblioteca generadora.
- Docker facilita reproducibilidad; los binarios portables permiten comprobar este equipo sin instalar Docker ni un servicio global.
- CI, despliegue, pagos reales y políticas configurables de clubes se mantienen fuera de M0.
