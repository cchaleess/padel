using Microsoft.EntityFrameworkCore;

namespace PadelMatch.Infrastructure.Persistence;

public sealed class PadelMatchDbContext(DbContextOptions<PadelMatchDbContext> options)
    : DbContext(options);
