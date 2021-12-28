# ImGui.Forms
A WinForms-inspired object-oriented framework around Dear ImGui.

## Get started
You only need to install the ``imGui.Forms`` nuget package in your project.

To create your own GUI, add the line ``Application.Create(new Form()).Execute();`` in your main method. This will create and execute an application and draw the content of ``Form``. You may derive from ``Form`` to create your own content.

``Application.Create`` takes in an ``ILocalizer`` as well, which can be called via ``Application.Localizer.Localize()`` to localize a string used in the application. Currently localizations get only set, when they are set explicitly. A system is planned, where you can set up automatic re-setting of application strings, after the locale of the ``ILocalizer`` was changed.

## Namespaces

### ImGui.Forms

Containing the ``Application`` and ``Form`` classes, which act as the main entry point into the library.

### ImGui.Forms.Controls

Containing all controls usable on a form.

You can create your own controls by deriving from ``ImGui.Forms.Controls.Base.Component`` and using ``ImGuiNET`` to emit Dear ImGui commands in the ``Update`` method. The ``Update`` method of the control will retrieve the final coordinates and size of itself as a parameter. Those information are derived from parent layouts and the ``Size`` value returned from ``GetSize`` in the same control.

### ImGui.Forms.Modals

Containing various modals and dialogs, such as ``MessageBox``, ``OpenFileDialog``, and ``SaveFileDialog``.

Use ``MessageBox.ShowErrorAsync``, ``MessageBox.ShowInformationAsync``, or ``MessageBox.ShowYesNoAsync`` to show a blocking messagebox over the remaining form content.

``OpenFileDialog`` and ``SaveFileDialog`` were designed to look and feel like the equally named dialogs in WinForms. Consult their Microsoft documentation for further information.

## Credits
ocurnut - For creating Dear ImGui
mellinoe - For creating ImGui.NET (bindings for Dear ImGui
Veldrid Team - For creating the Veldrid rendering pipeline
