using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetMigrator
{
    public interface ITargetTFMSelector
    {
        ValueTask<TargetFrameworkMoniker> SelectTFMAsync(IProject project);

        TargetFrameworkMoniker HighestPossibleTFM { get; }
    }
}
