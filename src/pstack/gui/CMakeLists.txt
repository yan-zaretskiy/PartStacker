add_executable(gui WIN32 MACOSX_BUNDLE
    app.cpp
    list_view.cpp
    main_window.cpp
    parts_list.cpp
    viewport.cpp
)

target_link_libraries(gui PRIVATE
    wx::net
    wx::core
    wx::base
    wx::gl
    pstack_calc
    pstack_files
    pstack_geo
    pstack_graphics
)
target_include_directories(gui PRIVATE "${PROJECT_SOURCE_DIR}/src")
add_dependencies(gui partstacker_copy_wx_headers)
