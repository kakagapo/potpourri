#include "bplustree.h"

using namespace index;

template <typename K, typename V>
V BPlusTree<K,V>::Get(K key)
{
    // Todo:
    // 1) Find the location in the leaf page and insert/replace the value
    // 2) Should mark the page as dirty so that we can flush it to disk later.
    Page* page = find_page(key);
    if(page != null){
        Entry* entry = page->find(key);
        if(entry != null){
            return (V&)entry->value;
        }
    }
    return null;    
    // Todo: Make this implementation thread-safe
}

template <typename K, typename V>
void BPlusTree<K,V>::Put(K key, V value)
{
    Page* page = find_page(key);
    add_to_page(page, key, value);
}

template <typename K, typename V>
bool BPlusTree<K,V>::Remove(K key)
{
    Page* page = find_page(key);
    if(page_contains(page, key)){
        remove_from_page(page, key);
    }
}

template <typename K, typename V>
bool BPlusTree<K,V>::Persist(){
    pageManager.FlushDirtyPages();
}
