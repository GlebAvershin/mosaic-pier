// Compiler-generated copies for the freestanding wasm32 target.
extern "C" void *memcpy(void *dst,const void *src,__SIZE_TYPE__ n){
    auto *d=static_cast<unsigned char*>(dst);auto *s=static_cast<const unsigned char*>(src);
    for(__SIZE_TYPE__ i=0;i<n;i++)d[i]=s[i];return dst;
}
extern "C" void *memset(void *dst,int value,__SIZE_TYPE__ n){
    auto *d=static_cast<unsigned char*>(dst);for(__SIZE_TYPE__ i=0;i<n;i++)d[i]=static_cast<unsigned char>(value);return dst;
}
