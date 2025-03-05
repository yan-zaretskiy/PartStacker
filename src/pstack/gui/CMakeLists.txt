add_executable(pstack_gui WIN32
    app.cpp
    list_view.cpp
    main_window.cpp
    parts_list.cpp
    viewport.cpp
)
target_sources(pstack_gui PUBLIC FILE_SET headers TYPE HEADERS FILES
    list_view.hpp
    main_window.hpp
    parts_list.hpp
    transformation.hpp
    viewport.hpp
)

set_target_properties(pstack_gui PROPERTIES
    PROJECT_LABEL "gui"
)
target_link_libraries(pstack_gui PRIVATE
    wx::net
    wx::core
    wx::base
    wx::gl
    pstack_calc
    pstack_files
    pstack_geo
    pstack_graphics
)
target_include_directories(pstack_gui PRIVATE "${PROJECT_SOURCE_DIR}/src")
add_dependencies(pstack_gui pstack_gui_copy_wx_headers)

set_target_properties(pstack_gui PROPERTIES OUTPUT_NAME "PartStackerGUI")

if(WIN32)
    target_sources(pstack_gui PRIVATE "${CMAKE_CURRENT_LIST_DIR}/../resource.rc")
elseif(APPLE)
    set_target_properties(pstack_gui PROPERTIES
        MACOSX_BUNDLE TRUE
        MACOSX_BUNDLE_INFO_PLIST "${CMAKE_CURRENT_LIST_DIR}/../Info.plist"
    )
endif()
