using System.Threading.Tasks;
using SharpHoundCommonLib;

namespace Sharphound.Client
{

    public interface Links<T>
    {
        IContext Initialize(IContext context, LdapConfig options);

        Task<IContext>
            TestConnection(
                T context);

        IContext SetSessionUserName(string overrideUserName, T context);
        IContext InitCommonLib(T context);
        Task<IContext> GetDomainsForEnumeration(T context);
        IContext StartBaseCollectionTask(T context);
        Task<IContext> AwaitBaseRunCompletion(T context);
        IContext StartLoopTimer(T context);
        IContext StartLoop(T context);
        Task<IContext> AwaitLoopCompletion(T context);
        IContext DisposeTimer(T context);
        IContext SaveCacheFile(T context);
        IContext Finish(T context);
    }
}