# UltraDES
UltraDES is a library for modeling, analysis and control of Discrete Event Systems. It has been developed at LACSED | UFMG (http://www.lacsed.eng.ufmg.br).

![UltraDES](http://lacsed.eng.ufmg.br/wp-content/uploads/2017/05/Logo_UltraDES_PNG_Internet-e1494353854950.png)

## Before using UltraDES

Requirements: 
- Supported OS: Windows, or virtual machine on Linux.
- Your computer must have a installed version of Microsoft Visual Studio (2014 or later version).
- Download the latest version of UltraDES (https://github.com/lacsed/UltraDES.git), by clicking on *Clone or Download* and then *Download Zip*.

## First steps

- Extract all the files of the zip or tar.gz file in your working directory. 
- Double click on the *UltraDES.sln* file.
- Congratulations, you are ready to use UltraDES!

## What is inside

Initially you are going to find four projects in the Solution Explorer:
- Other examples
- Test - Modular Supervisor
- Test - Monolithic Supervisor
- UltraDES (*non executable*)

To read the codes, click on the file *.cs* inside each project. 

## How to run the projects

- Before running the code, you need to set the project as Startup Project. 
    - On Solution Explorer select the project you want to run.
    - On the menu Project, select Set as Startup Project - Project>Set as Startup Project.
- To run the code, click on Debug menu - Debug>Start Debugging.

## Creating a new project

- On the menu File, select New>Project.
- On the New Project window:
  - Select Visual C#>Console App (.NET Framework).
  - Give your project a name.
  - The location of your project should be the same as the other UltraDES projects.
  - In the Solution field, choose *Add to Solution*.
  - Click on OK to create your project.
- Remember to set your project as Startup Project (see How to run the projects). 
- Congratulations, you just created your project!

## Inside your project

- In the Solution Explorer, double click on your project.
- Inside your project, right click on the item References>Add Reference...
  - On the Reference Manager window, select Projects>Solution and select UltraDES.
  - Click Ok to close the window.
- To write your code, click on the file *.cs* inside your project. 
- On the *.cs* file of your project, add the UltraDES library in the header (```using UltraDES;```).

In your *main* function, add the code below to create and see your first Automaton.

### Creating States

```cs
State s1 = new State("s1", Marking.Marked);
State s2 = new State("s2", Marking.Unmarked);
```

### Creating Events

```cs
Event e1 = new Event("e1", Controllability.Controllable);
Event e2 = new Event("e2", Controllability.Uncontrollable);
```

### Creating Transitions

```cs
var t = new Transition(s1, e1, s2);
```

### Creating an Automaton

```cs
var G = new DeterministicFiniteAutomaton(new[]
  {
    new Transition(s1, e1, s2), 
    new Transition(s2, e2, s1)
  }, s1, "G");
```

### Showing the Automaton

```cs
 G.drawSVGFigure("G", true);
 ```

## Other examples

For more examples on how to create more complex automata or how to use the methods, read the other projects. 

