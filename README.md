# BedrockService
Windows Service Wrapper around Bedrock Server

There is a Windows Server Software for Windows to allow users to run a multiplayer minecraft server.

You can get it at https://www.minecraft.net/en-us/download/server/bedrock/

Its easy to install and runs as a console application.

What if you want it to run invisibly on your computer whenever it starts and shutdown statefully whenever your computer shuts?

Enter BedrockService, the little control program that performs just that task for you.

To configure it you have to do two things:

1.  You have to put the path to your copy of bedrock_server.exe in the app.config file.

2.  You need to give permissions to NETWORK_SERVICE to Modify both the directory with BedrockService as well as the directory containing bedrock_server.exe
