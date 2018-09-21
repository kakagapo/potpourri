cmake_minimum_required (VERSION 2.6)
project (HelloWorld)
add_subdirectory(src)
 
# add the binary tree to the search path for include files
# so that we will find HelloWorldConfig.h
include_directories("${PROJECT_BINARY_DIR}")