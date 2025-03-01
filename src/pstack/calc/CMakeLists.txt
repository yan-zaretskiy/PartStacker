add_library(pstack_calc STATIC
    sinterbox.cpp
    voxelize.cpp
)

target_link_libraries(pstack_calc
    PUBLIC pstack_geo
)
target_include_directories(pstack_calc PRIVATE "${PROJECT_SOURCE_DIR}/src")
