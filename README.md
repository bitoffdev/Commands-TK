Commands - Unity Code Editor and Executor
=======================================
Copyright 2014 EJM Software

This is a simple Unity solution for editing code in the editor. It also has Syntax highlighting.

----------------------
#Features
 - Edit code in the editor
 (up to 16,382 characters)
 - Load and Save files
 - C# Syntax Highlighting
 - Run static functions in the editor
 (Note that arguments are not supported yet)

-----------------------
#FAQ

1.  I get the error "error CS0234: The type or namespace name `CSharp' does not exist in the namespace `Microsoft'. Are you missing an assembly reference?"

    Fix: Access the “File | Build Setting” menu in the Unity 3.x Editor, ensure that “PC and Mac Standalone” is selected for the Platform, and then click the “Player Settings...” button. In the Inspector window, under Optimization, change the “API Compatibility Level” value from “.NET 2.0 Subset” to “.NET 2.0”. To force a code rebuild you might have to right-click on the CSharpInterpreter script in the Project window and select Reimport.
