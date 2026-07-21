using LegacyTaskManager.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LegacyTaskManager.Api.Tests.TestHelpers
{
    public static class TestDbContextFactory
    {
        // Fresh, isolated in-memory database per call so tests never see each other's data
        // and never touch the real Sqlite database configured in Program.cs.
        public static AppDbContext Create()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }
    }
}
