#ifndef MOCK_STORE_BPLUS_TREE_H_
#define MOCK_STORE_BPLUS_TREE_H_

#include <string>

namespace index{
    // As first iteration it will be implemented as a sorted string table.
    class BPlusTree
    {
        public:
            void AddEntry(std::string key, std::string value);
            std::string GetEntry(std::string key);
            void RemoveEntry(std::string key);
            int Count();
    };
};

#endif