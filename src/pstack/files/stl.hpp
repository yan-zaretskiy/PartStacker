#ifndef PSTACK_FILES_STL_HPP
#define PSTACK_FILES_STL_HPP

#include "pstack/calc/mesh.hpp"
#include <string>

namespace pstack::files {

calc::mesh from_stl(const std::string& from_file);
void to_stl(const calc::mesh& mesh, const std::string& to_file);

} // namespace pstack::files

#endif // PSTACK_FILES_STL_HPP
