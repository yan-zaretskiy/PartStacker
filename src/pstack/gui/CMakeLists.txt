add_executable(pstack_gui WIN32 MACOSX_BUNDLE
    app.cpp
    list_view.cpp
    main_window.cpp
    parts_list.cpp
    viewport.cpp
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
