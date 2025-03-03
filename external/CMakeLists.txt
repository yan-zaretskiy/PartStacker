set(wxBUILD_SHARED OFF)
set(wxBUILD_CXX_STANDARD 20)
add_subdirectory(wxWidgets)

set(GLEW_USE_STATIC_LIBS ON)
add_subdirectory(glew/build/cmake)

if(WIN32)
    if(CMAKE_BUILD_TYPE STREQUAL "Debug")
        set(PARTSTACKER_COPY_WXWIDGETS_HEADERS_DIRECTORY "mswud")
    elseif(CMAKE_BUILD_TYPE MATCHES "Release")
        set(PARTSTACKER_COPY_WXWIDGETS_HEADERS_DIRECTORY "mswu")
    endif()
    add_custom_target(pstack_gui_copy_wx_headers
        COMMAND ${CMAKE_COMMAND} -E copy
            "${CMAKE_CURRENT_BINARY_DIR}/wxWidgets/lib/vc_x64_lib/${PARTSTACKER_COPY_WXWIDGETS_HEADERS_DIRECTORY}/wx/setup.h"
            "${PROJECT_SOURCE_DIR}/external/wxWidgets/lib/vc_x64_lib/${PARTSTACKER_COPY_WXWIDGETS_HEADERS_DIRECTORY}/wx/setup.h"
    )
endif()

get_all_targets(external_targets "${CMAKE_CURRENT_LIST_DIR}")
foreach(target ${external_targets})
    get_target_property(folder ${target} FOLDER)
    if("${folder}" STREQUAL "CMakePredefinedTargets")
        continue()
    endif()
    if("${folder}" MATCHES "-NOTFOUND$")
        set(folder "external")
    else()
        set(folder "external/${folder}")
    endif()
    set_target_properties(${target} PROPERTIES FOLDER ${folder})
endforeach()
