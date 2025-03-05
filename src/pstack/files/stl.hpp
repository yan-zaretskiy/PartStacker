#ifndef PSTACK_FILES_STL_HPP
#define PSTACK_FILES_STL_HPP

#include "pstack/calc/mesh.hpp"
#include <string>

namespace pstack::files {

calc::mesh from_stl(const std::string& file_path);
void to_stl(const calc::mesh& mesh, const std::string& file_path);

} // namespace pstack::files

#endif // PSTACK_FILES_STL_HPP
