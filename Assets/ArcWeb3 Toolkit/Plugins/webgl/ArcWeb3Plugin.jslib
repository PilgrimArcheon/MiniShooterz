var ArcWeb3Plugin = {
    IsWebMobile: function()
    {
        return Module.SystemInfo.mobile;
    },
    
    InitArcWeb3JS: function (configJson) {
        if(window.arcWeb3Bridge && configJson) {
            window.arcWeb3Bridge.initializeConfig(UTF8ToString(configJson));
        }
    },

    ConnectArcWalletJS: function () {
        if(window.arcWeb3Bridge) window.arcWeb3Bridge.connectWallet();
    },

    ArcGenericReadJS: function (contract, abi, method, args) {
        window.arcWeb3Bridge.genericRead(
            contract ? UTF8ToString(contract) : "", 
            abi ? UTF8ToString(abi) : "", 
            method ? UTF8ToString(method) : "", 
            args ? UTF8ToString(args) : "" 
        );
    },

    ArcGenericWriteJS: function (contract, abi, method, args, value) {
        window.arcWeb3Bridge.genericWrite(
            contract ? UTF8ToString(contract) : "", 
            abi ? UTF8ToString(abi) : "", 
            method ? UTF8ToString(method) : "", 
            args ? UTF8ToString(args) : "",
            value ? UTF8ToString(value) : "0"
        );
    }
};

mergeInto(LibraryManager.library, ArcWeb3Plugin); 