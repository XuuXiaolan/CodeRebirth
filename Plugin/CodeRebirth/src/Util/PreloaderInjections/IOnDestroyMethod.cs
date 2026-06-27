using InjectionLibrary.Attributes;

[assembly: RequiresInjections]

namespace CodeRebirth.src.Util.PreloaderInjections;

[HandleErrors(InjectionLibrary.ErrorHandlingStrategy.Ignore)]
[InjectInterface(typeof(EntranceTeleport))]
interface IOnDestroyMethod
{
    [HandleErrors(InjectionLibrary.ErrorHandlingStrategy.Ignore)]
    void OnDestroy();
}