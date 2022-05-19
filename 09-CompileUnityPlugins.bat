@echo off

call MC7D2D UnityPlugins\ElectricityWindPower.dll ^
/reference:"%PATH_7D2D_MANAGED%\Assembly-CSharp.dll" ^
UnityPlugins\*.cs && ^
echo Successfully compiled UnityPlugins\ElectricityWindPower.dll

pause