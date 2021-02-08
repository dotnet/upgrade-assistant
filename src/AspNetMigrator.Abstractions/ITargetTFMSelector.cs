using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetMigrator
{
    public interface ITargetTFMSelector
    {
        /// <summary>
        /// Chooses the most likely target TFM a project should be retargeted to based on its style, output type, dependencies, and
        /// the user's preference of current or LTS.
        /// </summary>
        ValueTask<TargetFrameworkMoniker> SelectTFMAsync(IProject project);
    }
}
