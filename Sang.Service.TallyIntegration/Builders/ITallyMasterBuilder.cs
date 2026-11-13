using Sang.Service.TallyIntegration.Models;

namespace Sang.Service.TallyIntegration.Builders
{
    public interface ITallyMasterBuilder
    {
        string Build(TallyMasterRequest request);
    }
}