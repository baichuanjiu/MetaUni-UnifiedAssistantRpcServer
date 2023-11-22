# 目录

- [目录](#目录)
- [写在前面](#写在前面)
- [Auth.RPC](#authrpc)
- [Feed.RPC](#feedrpc)
- [Message.RPC](#messagerpc)
- [User.RPC](#userrpc)

# 写在前面

小程序调用RPC接口时，需要在Header中带上以下字段：
```
{
    "id": "小程序ID",
    "jwt": "颁发给小程序的JWT",
}
```

# Auth.RPC

提供JWT验证功能，用于验证用户传入的UUID与JWT是否合法。

# Feed.RPC

提供推送功能，可将小程序内容推送至客户端“推荐”页。

# Message.RPC

提供向用户发送消息功能，目前可发送私聊请求与通知两类消息，调用此接口前，请先确保小程序已注册SystemPromotion。

# User.RPC

提供获取用户信息功能，可获取用户的头像与网名。