using InjectionLibrary.Attributes;

[assembly: RequiresInjections]

namespace CodeRebirth.src.Util.PreloaderInjections;

[HandleErrors(InjectionLibrary.ErrorHandlingStrategy.Ignore)]
[InjectInterface(typeof(QuicksandTrigger))]
interface IAwakeMethod
{
    [HandleErrors(InjectionLibrary.ErrorHandlingStrategy.Ignore)]
    void Awake();
}