#define UNICODE

#include <windows.h>
#include <initguid.h>
#include <unknwn.h>

// {7CEE7D1B-5E02-4546-9283-1A49408EFD51}
DEFINE_GUID(IID_IAnkhComAggregator, 
0x7cee7d1b, 0x5e02, 0x4546, 0x92, 0x83, 0x1a, 0x49, 0x40, 0x8e, 0xfd, 0x51);

// {9FD6D94E-2223-4578-AD8C-05A926FCF292}
DEFINE_GUID(CLSID_AnkhComAggregator, 
0x9fd6d94e, 0x2223, 0x4578, 0xad, 0x8c, 0x5, 0xa9, 0x26, 0xfc, 0xf2, 0x92);

UINT DllRefCount = 0;
HINSTANCE ModuleInstance = NULL;

// The class factory

class AnkhComAggregatorClassFactory : public IClassFactory
{
    
public:
    AnkhComAggregatorClassFactory();
    ~AnkhComAggregatorClassFactory();

    //IUnknown members
    STDMETHODIMP         QueryInterface(REFIID, LPVOID FAR *);
    STDMETHODIMP_(ULONG) AddRef();
    STDMETHODIMP_(ULONG) Release();
    
    //IClassFactory members
    STDMETHODIMP      CreateInstance(LPUNKNOWN, REFIID, LPVOID FAR *);
    STDMETHODIMP      LockServer(BOOL);
private:
    int refCount;
};

class IAnkhComAggregator : public IUnknown
{
public:
    STDMETHOD(SetObject)(IUnknown* punk) = 0;
};

class AnkhComAggregator : public IAnkhComAggregator
{
public:
    AnkhComAggregator() : refCount(0), innerObject(NULL)
    {;}

    //IUnknown members
    STDMETHODIMP         QueryInterface(REFIID, LPVOID FAR *);
    STDMETHODIMP_(ULONG) AddRef();
    STDMETHODIMP_(ULONG) Release();

    // Our only method
    STDMETHODIMP SetObject( IUnknown* punk );
   /* STDMETHODIMP Foo()
    {
        return NOERROR;
    }*/

private:
    int refCount;
    IUnknown* innerObject;

};

AnkhComAggregatorClassFactory::AnkhComAggregatorClassFactory()
{
    DllRefCount++;
    this->refCount = 0;
}


AnkhComAggregatorClassFactory::~AnkhComAggregatorClassFactory()
{
    DllRefCount--;
}

STDMETHODIMP_(ULONG) AnkhComAggregatorClassFactory::AddRef()
{
    return this->refCount++;
}

STDMETHODIMP_(ULONG) AnkhComAggregatorClassFactory::Release()
{
    if ( --this->refCount > 0 )
        return this->refCount;
    else
    {
        delete this;
        return 0L;
    }
}

STDMETHODIMP AnkhComAggregatorClassFactory::QueryInterface(REFIID riid,
                                                   LPVOID FAR *ppv)
{
    *ppv = NULL;

    // Any interface on this object is the object pointer

    if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_IClassFactory))
    {
        *ppv = (LPCLASSFACTORY)this;

        AddRef();

        return NOERROR;
    }

    return E_NOINTERFACE;
}  

STDMETHODIMP AnkhComAggregatorClassFactory::CreateInstance(LPUNKNOWN pUnkOuter,
                                                           REFIID riid,
                                                           LPVOID* ppvObj)
{
    *ppvObj = NULL;

    if ( pUnkOuter )
        return CLASS_E_NOAGGREGATION;

    AnkhComAggregator* aggregator = new AnkhComAggregator;
    return aggregator->QueryInterface(riid, ppvObj);
}

STDMETHODIMP AnkhComAggregatorClassFactory ::LockServer(BOOL /*fLock*/)
{
    return NOERROR;
}

STDMETHODIMP_(ULONG) AnkhComAggregator::AddRef()
{
    return this->refCount++;
}

STDMETHODIMP_(ULONG) AnkhComAggregator::Release()
{
    --this->refCount;
    if ( this->refCount > 0 )
        return this->refCount;
    else
    {
        delete this;
        return 0L;
    }
}

STDMETHODIMP AnkhComAggregator::QueryInterface(REFIID riid, LPVOID FAR *ppv)
{
    if ( IsEqualIID(riid, IID_IAnkhComAggregator) || IsEqualIID(riid, IID_IUnknown) )
    {
        *ppv = this;
        this->AddRef();
        return NOERROR;
    }
    else
    {
        if ( this->innerObject != NULL )
        {
            return this->innerObject->QueryInterface(riid, ppv);
        }
        else
        {
            return E_UNEXPECTED;
        }
    }

}

STDMETHODIMP AnkhComAggregator::SetObject(IUnknown* punk)
{
    this->innerObject = punk;
    return NOERROR;
}



// The DLL entry point
extern "C" int APIENTRY
DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID /* lpReserved */)
{
    if (dwReason == DLL_PROCESS_ATTACH)
    {
        // Extension DLL one-time initialization
        ModuleInstance = hInstance;
    }
    else if (dwReason == DLL_PROCESS_DETACH)
    {
    }

    return 1;   // ok
}

// Ok to unload this DLL?
STDAPI DllCanUnloadNow(void)
{
    return (DllRefCount == 0 ? S_OK : S_FALSE);
}

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID *ppvOut)
{
    *ppvOut = NULL;

    if ( IsEqualIID(rclsid, CLSID_AnkhComAggregator) )
    {
        AnkhComAggregatorClassFactory* factory = new AnkhComAggregatorClassFactory;
        return factory->QueryInterface( riid, ppvOut );
    }
    else
    {
        return CLASS_E_CLASSNOTAVAILABLE;
    }
}



STDAPI DllRegisterServer()
{
    
    WCHAR lpszFileName[MAX_PATH+1];
    HMODULE hMod = GetModuleHandle(L"ComAggregator");
    HKEY hKey1, hKey2, hKey3, hKey4, hKey5;

    GetModuleFileName(hMod, lpszFileName, MAX_PATH+1);
    RegOpenKey(HKEY_CLASSES_ROOT, L"CLSID", &hKey1);
    RegCreateKey(hKey1, L"{9FD6D94E-2223-4578-AD8C-05A926FCF292}", &hKey2);
    RegCreateKey(hKey2, L"InProcServer32", &hKey3);
    RegSetValueEx(hKey3, NULL, 0, REG_SZ, reinterpret_cast<LPBYTE>(lpszFileName), 2 * (lstrlen(lpszFileName) + 1));
    RegSetValueEx(hKey3, L"ThreadingModel", 0, REG_SZ, reinterpret_cast<LPBYTE>(L"Both"), 20);
    RegCloseKey(hKey3);
    RegCloseKey(hKey2);
    RegCloseKey(hKey1);

    

    RegCreateKey(HKEY_CLASSES_ROOT, L"Ankh.ComAggregator", &hKey4);
    RegCreateKey(hKey4, L"CLSID", &hKey5);
    RegSetValueEx(hKey5, NULL, 0, REG_SZ, reinterpret_cast<LPBYTE>(L"{9FD6D94E-2223-4578-AD8C-05A926FCF292}"), 
        2* (lstrlen(L"{9FD6D94E-2223-4578-AD8C-05A926FCF292}") + 1));
    RegCloseKey(hKey5);
    RegCloseKey(hKey4);
    
    return S_OK;
}

STDAPI DllUnregisterServer()
{
    HKEY hKey1, hKey2;
    
    RegOpenKey(HKEY_CLASSES_ROOT, L"CLSID", &hKey1);
    RegOpenKey(hKey1, L"{9FD6D94E-2223-4578-AD8C-05A926FCF292}", &hKey2);
    RegDeleteKey(hKey2, L"InProcServer32");
    RegCloseKey(hKey2);
    RegDeleteKey(hKey1, L"{9FD6D94E-2223-4578-AD8C-05A926FCF292}");
    RegCloseKey(hKey1);

    RegOpenKey(HKEY_CLASSES_ROOT, L"Ankh.ComAggregator", &hKey1);
    RegDeleteKey(hKey1, L"CLSID");
    RegCloseKey(hKey1);
    RegDeleteKey(HKEY_CLASSES_ROOT, L"Ankh.ComAggregator");

    return S_OK;
}