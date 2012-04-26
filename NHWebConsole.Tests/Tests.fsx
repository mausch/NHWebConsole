#r "bin\Debug\Fuchu.dll"
#r "bin\Debug\NHWebConsole.Tests.exe"

open Fuchu
open NHWebConsole.Tests

(*
Tests.AllTests
|> Test.filter (fun n -> n.Contains "null component")
|> run
*)

run Tests.AllTests
