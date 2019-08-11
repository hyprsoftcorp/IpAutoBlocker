using System.Collections.Generic;

namespace Hyprsoft.Cloud.Utilities
{
    public interface IValidatable
    {
        IEnumerable<string> IsValid();
    }
}
