@echo off
echo ���ڷ����ͻ�����Ŀ...
pushd FivemPlayerlist
dotnet publish -c Release
popd

echo ���ڷ�����������Ŀ...
pushd FivemPlayerlistServer
dotnet publish -c Release
popd

echo ����ɵķ����ļ���...
rmdir /s /q dist
mkdir dist

echo ���� fxmanifest.lua �ļ�...
copy /y fxmanifest.lua dist

echo ���ƿͻ�����Ŀ�ļ�...
xcopy /y /e Client\bin\Release\net452\publish dist\Client\bin\Release\net452\publish\

echo ���Ʒ�������Ŀ�ļ�...
xcopy /y /e Server\bin\Release\netstandard2.0\publish dist\Server\bin\Release\netstandard2.0\publish\

echo �������
pause