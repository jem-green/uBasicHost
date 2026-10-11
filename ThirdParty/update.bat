rmdir .\tracer\ /S /Q
xcopy c:\source\git\cs.net\tracer\tracerlibrary\bin\debug\ .\tracer\ /S /Y

rmdir .\ubasic-library
xcopy C:\Users\jemgr\source\repos\uBasic\ .\ubasic\ /S /Y
xcopy C:\Users\jemgr\source\repos\uBasic\ubasic.dll ..\ubasicconsole\ /S /Y
xcopy C:\Users\jemgr\source\repos\uBasic\ubasic.dll ..\ubasicform\ /S /Y
xcopy C:\Users\jemgr\source\repos\uBasic\ubasic.dll ..\ubasicapp\ /S /Y

rmdir .\display\ /S /Q
xcopy c:\source\git\cs.net\display\displaylibrary\bin\debug\ .\display\ /S /Y

rmdir .\rasterfont\ /S /Q
xcopy c:\source\git\cs.net\rasterfont\rasterfontlibrary\bin\debug\ .\rasterfont\ /S /Y

rmdir .\vectorfont\ /S /Q
xcopy c:\source\git\cs.net\vectorfont\vectorfontlibrary\bin\debug\ .\vectorfont\ /S /Y

rmdir .\colour\ /S /Q
xcopy c:\source\git\cs.net\colour\colourlibrary\bin\debug\ .\colour\ /S /Y
