add_library(pstack_graphics STATIC
    buffer.cpp
    global.cpp
    shader.cpp
)

target_link_libraries(pstack_graphics
    PUBLIC pstack_geo
    PRIVATE glew_s
)
target_include_directories(pstack_graphics PRIVATE
    "${PROJECT_SOURCE_DIR}/src"
    "${PROJECT_SOURCE_DIR}/external/glew/include"
)
