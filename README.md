# TAPL
C# implementation of Benjamin Pierce's *Types and Programming Languages*

Currently working on fullfomsub. The rest of the modules are "complete" in the sense they seem to be functioning as intended according to the thorough testing suite written in Abstract-Syntax. I plan to make a lexer/parser using GPPG for fullfomsub.

The format I chose to represent the algebraic data types is a tagged union. This is because I took the code from [here](https://github.com/jack-pappas/fsharp-tapl) and when you compile the F# into a class library, then decompile it into C# you will see that the algebraic data types become tagged unions. But if you were to interface this class library in a C# program, you have to use ugly "X is class" syntax, aka switch on the type at runtime, and I just thought it would be easier to use a tagged union since it's basically that except your tags are the class types themselves. Plus that's what one would do if they were to implement it in C as an actual union. 
