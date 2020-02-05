# BedrockService
Windows Service Wrapper around Bedrock Server for Minecraft

Lets you run Bedrock Server as a Windows Service

This approach does NOT require Docker.

There is a Windows Server Software for Windows to allow users to run a multiplayer Minecraft server.

You can get it at https://www.minecraft.net/en-us/download/server/bedrock/

Its easy to install and runs as a console application.

What if you want it to run invisibly on your computer whenever it starts and shutdown statefully whenever your computer shuts?

Enter BedrockService, the little control program that performs just that task for you.  Download it here: https://github.com/ravetroll/BedrockService/raw/master/Releases/BedrockService.exe.zip

To configure it you have to do 4 things:

1.  Unzip the BedrockService.exe zip to a directory on your computer.

2.  You have to put the path to your copy of bedrock_server.exe in the BedrockService.exe.config file.  Make sure you have run your bedrock server in console mode first to be sure it works.

3.  You need to give permissions to SYSTEM to Modify both the directory with BedrockService as well as the directory containing bedrock_server.exe

4.  Start a command prompt console with admin priviledges and navigate to the directory where you unzipped BedrockService.  
```
    Type: bedrockservice install   
    then
    Type: bedrockservice start
```    
If you need to uninstall BedrockService Start a command prompt console with admin priviledges and navigate to the directory where you unzipped BedrockService.
```
    Type: bedrockservice stop
    then
    Type: bedrockservice uninstall
```    

If you have some problems getting the service running Check in Windows Event Log in the Application Events for events related to BedrockService.  That might help you find the problem.
