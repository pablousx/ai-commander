# Guía: Cómo crear un nuevo Provider para AI Commander

Para añadir soporte a un nuevo agente de IA (ej. un nuevo IDE o herramienta), necesitas crear una clase que herede de `BaseProvider`.

## Pasos

1. **Crea el archivo del provider** en `src/AICommander.Core/Providers/Nuevo/NuevoProvider.cs`.
2. **Implementa la clase** heredando de `BaseProvider`:
   ```csharp
   namespace AICommander.Core.Providers.Nuevo
   {
       public class NuevoProvider : BaseProvider
       {
           // El nombre que se usará en el YAML (ej: "nuevo")
           public override string Name => "nuevo";
           
           // Opcional: si necesitas lógica personalizada para detectar
           // si está corriendo, puedes sobrescribir IsRunning() o IsVisible()
       }
   }
   ```
3. **Registra el provider** en `src/AICommander.App/App.xaml.cs`:
   ```csharp
   var providers = new List<IProvider>
   {
       new AntigravityProvider(),
       new VSCodeProvider(),
       new ClaudeProvider(),
       new NuevoProvider() // <-- Agrega tu provider aquí
   };
   ```
4. **Agrega configuración por defecto** en `config/ai-commander.yaml`:
   ```yaml
   providers:
     nuevo:
       enabled: true
       process_name: "proceso_nuevo"
       actions:
         accept:
           key_sequence: ["Ctrl+Enter"]
   ```

El `BaseProvider` se encarga de poner en foco la ventana del proceso `process_name` y enviarle la `key_sequence` correspondiente a la acción, por lo que en la mayoría de los casos no necesitas escribir nada de lógica en tu provider.
