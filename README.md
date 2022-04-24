# ImGui.Forms
A WinForms-inspired object-oriented framework around Dear ImGui.

## Get started
You only need to install the ``imGui.Forms`` nuget package in your project.

To create your own GUI, you have to add those two lines to your main method:
```
public static void Main()
{
  var form = new MainForm();
  new Application().Execute(form);
}
```

``MainForm`` is your own derivation of the abstract class ``Form``, in which you set your components for your design. ``MainForm`` is used as an example name, and you can freely choose the name of your derivative class.

The constructor of ``Application`` takes an ``ILocalizer`` as well, which can be called via ``Application.Localizer.Localize()`` to localize a string used in the application. Currently localizations get only set, when they are set explicitly. A system is planned, where you can set up automatic re-setting of application strings, after the locale of the ``ILocalizer`` was changed.

## Namespaces

### ImGui.Forms

Containing the ``Application`` and ``Form`` classes, which act as the main entry point into the library.

### ImGui.Forms.Controls

Containing all controls usable on a form.

You can create your own controls by deriving from ``ImGui.Forms.Controls.Base.Component`` and using ``ImGuiNET`` to emit Dear ImGui commands in the ``UpdateInternal`` method. The ``UpdateInternal`` method of the control will retrieve the absolute coordinates and size of itself as a parameter, as they would be needed in ImGui DrawList calls. Those information are derived from parent layouts and the ``Size`` value returned from ``GetSize`` in the same control.

### ImGui.Forms.Modals

Containing various modals and dialogs, such as ``MessageBox``, ``OpenFileDialog``, and ``SaveFileDialog``.

Use ``MessageBox.ShowErrorAsync``, ``MessageBox.ShowInformationAsync``, or ``MessageBox.ShowYesNoAsync`` to show a blocking messagebox over the remaining form content.

``OpenFileDialog`` and ``SaveFileDialog`` were designed to look and feel like the equally named dialogs in WinForms. Consult their Microsoft documentation for further information.

## Credits
ocurnut - For creating Dear ImGui
mellinoe - For creating ImGui.NET (bindings for Dear ImGui)
Veldrid Team - For creating the Veldrid rendering pipeline
