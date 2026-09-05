using InjectionLibrary.Attributes;

[assembly: RequiresInjections]

namespace CodeRebirth.src.Util.PreloaderInjections;

[HandleErrors(InjectionLibrary.ErrorHandlingStrategy.Ignore)]
[InjectInterface(typeof(EntranceTeleport))]
[InjectInterface(typeof(QuicksandTrigger))]
interface IOnDestroyMethod
{
    [HandleErrors(InjectionLibrary.ErrorHandlingStrategy.Ignore)]
    void OnDestroy();
}