@echo off
echo 正在发布客户端项目...
pushd FivemPlayerlist
dotnet publish -c Release
popd

echo 正在发布服务器项目...
pushd FivemPlayerlistServer
dotnet publish -c Release
popd

echo 清理旧的发布文件夹...
rmdir /s /q dist
mkdir dist

echo 复制 fxmanifest.lua 文件...
copy /y fxmanifest.lua dist

echo 复制客户端项目文件...
xcopy /y /e Client\bin\Release\net452\publish dist\Client\bin\Release\net452\publish\

echo 复制服务器项目文件...
xcopy /y /e Server\bin\Release\netstandard2.0\publish dist\Server\bin\Release\netstandard2.0\publish\

echo 发布完成
pause