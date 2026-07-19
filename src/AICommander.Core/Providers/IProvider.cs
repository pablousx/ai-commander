using System.Threading.Tasks;
using AICommander.Core.Config;

namespace AICommander.Core.Providers
{
    public interface IProvider
    {
        string Name { get; }
        string ProcessName { get; }
        void Initialize(ProviderConfig config);
        bool IsRunning();
        bool IsVisible();
        
        /// <summary>
        /// Ejecuta una acción enviando las teclas de acción del provider
        /// a la ventana objetivo. Las teclas vienen de la config YAML,
        /// NO de la hotkey que presionó el usuario.
        /// </summary>
        Task ExecuteAction(string actionName, ActionConfig actionConfig);
    }
}
