﻿using System;

namespace AspNetMigrator
{
    public static class ProjectExtensions
    {
        public static UpgradeProjectInfo Required(this UpgradeProjectInfo? project)
            => project ?? throw new InvalidOperationException("Project cannot be null");
    }
}
