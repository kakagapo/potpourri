cmake_minimum_requddired(VERSION 2.8)
project(storage VERSION 0.1 LANGUAGES CXX)

set(EXTERNAL ${CMAKE_SOURCE_DIR}/third_party)
set(LIBRARIES ${CMAKE_SOURCE_DIR}/libs)
set(STORAGE_ENGINE ${CMAKE_SOURCE_DIR}/src)

add_subdirectory(${EXTERNAL})
add_subdirectory(${LIBRARIES})
add_subdirectory(${LIBRARIES})
add_subdirectory(${STORAGE_ENGINE})
