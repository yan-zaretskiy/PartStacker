add_library(pstack_files STATIC
    read.cpp
    stl.cpp
)

target_link_libraries(pstack_files
    PUBLIC pstack_geo
)
target_include_directories(pstack_files PRIVATE "${PROJECT_SOURCE_DIR}/src")
