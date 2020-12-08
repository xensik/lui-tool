local _project_folder = path.getabsolute("src")

function project_folder()
    return path.getrelative(os.getcwd(), _project_folder)
end
-------------------------------------------------
workspace "lui-tool"
location "./build"
objdir "%{wks.location}/obj/%{cfg.buildcfg}/%{prj.name}"
targetdir "%{wks.location}/bin/%{cfg.buildcfg}"
targetname "%{prj.name}"

language "C++"
cppdialect "C++17"
architecture "x86_64"

filter "action:vs*"
    buildoptions "/Zc:__cplusplus"
filter{}

configurations { "debug", "release", }

symbols "On"

configuration "release"
    optimize "Full"
    defines { "NDEBUG", "YY_NO_UNISTD_H"}
configuration{}

configuration "debug"
    optimize "Debug"
    defines { "DEBUG", "_DEBUG", "YY_NO_UNISTD_H" }
configuration {}

startproject "lui-tool"
-------------------------------------------------
include "src/tool.lua"
include "src/utils.lua"
include "src/IW6.lua"
tool:project()
utils:project()
IW6:project()
