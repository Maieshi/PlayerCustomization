mergeInto(LibraryManager.library, {
    
    Start_Event: function(message)
    {
        try
        {
            __UnityLib__.StartEvent(UTF8ToString(message))
        }
        catch (e)
        {
            console.error(e)
        }   
    },

    Error_Message: function(message)
    {
        try
        {
            __UnityLib__.ErrorMessage(UTF8ToString(message))
        }
        catch (e)
        {
            console.error(e)
        }   
    },

    Call_Back: function(message)
    {
        try
        {
            __UnityLib__.Callback(UTF8ToString(message))
        }
        catch (e)
        {
            console.error(e)
        }   
    },


});