# unity3d automation playground

## Overview 
This is a playground I have thrown together to better understand the cost / complexities of creating an inhouse solution for build and testing a Unity3D mobile game. At a stretch this could also be a template to quickly get build and test automation up and running.

![](/documentation/overview_diagram.png)

## Current Features
- Jenkins setup using configuration as code
- Unity Personal License activation in Jenkins
- Connection Bridge
- Local Android Automation 

## Current Limitations
- Only supports Linux as a Host

## Setup
1. Create a `.env` file in the `infrastructure` directory, this file should contain the required environment variables. 
```
GITHUB_API_TOKEN=********
UNITY_USERNAME=********
UNITY_PASSWORD=********
```
2. Run `docker compose up` from the `infrastructure` directory to startup the infrastructure part of the solution.
3. Run the `unity-license` job in jenkins, this can be found at `http://127.0.0.1:8080/`
4. Now you should be able to freely run `unity` and `unity-appium` jobs as desired.

## Todo
- documentation (just some markdown files)
- support android emulator via docker image in appium 
- support for cloud appium devicefarm eg. `BrowserStack` or `SauceLabs`
- support OSX and Windows as Hosts
- remote debugger tool
- extend frameworks querying functionality 
- iOS Build support 
- iOS Test support
- Windows Build support 
- Windows Test support 
- Realtime Test Framework support
- Upload to GooglePlay - https://plugins.jenkins.io/google-play-android-publisher/
- Validate AAB / APK 
- Unity Package for Unity Runtime Code
- Nuget Package for C# Test Framework Code

## Note
- https://medium.com/@rosaniline/setup-chained-jenkins-declarative-pipeline-projects-with-triggers-d3d04f1daf75

