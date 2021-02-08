using NSubstitute;

namespace AspNetMigrator.Test
{
    public static class TestHelpers
    {
        public static IMigrationContext GetTestContext()
        {
            var context = Substitute.For<IMigrationContext>();

            // Clear the project so that test migration steps don't run at both the project and solution level
            context.CurrentProject.Returns((UpgradeProjectInfo?)null);

            return context;
        }
    }
}
