#ifndef MOCK_STORE_BPLUS_TREE_H_
#define MOCK_STORE_BPLUS_TREE_H_

#include <string>
#include <type_traits>

using namespace std;

namespace index
{
    // Todo: figure out how to handle address format change
    struct FileAddress
    {
        string file;
        unsigned int offset;
    };

    // 
    template <typename K>
    struct Page
    {
        char version;
        size_t length;
        Entry* entries;
    };

    template <typename K, typename V>
    struct LeafPage : public Page
    {
        private:
            Page* prevLeafPage;
            Page* nextLeafPage;

        public:
            Entry* find(K key);
            void Add(K key, V value);
            void Put(K key, V value);
    };

    template <typename K, typename Addr>
    struct Entry
    {
        K key;
        Addr address;
    };

    // As first iteration it will be implemented as a sorted string table.
    template <typename K, typename V>
    class BPlusTree
    {
        public:
            void Put(K key, V value);
            V Get(K key);
            bool Remove(K key);
            int Count();
            size_t Height();
        
        protected:
            Page<K> Root();
            bool Persist();

        private:
            PageManager pageManager;
            
    };
};

#endif