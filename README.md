# Hydra 
The basic component using c# when CS development, it provider some useful compoent, inlcude CRC32、Log and Simple RPC base Json !


## Why I do this
Long long ago, when I want to do something, should thinking myself and ask someone who experience in this domain, it very very cost time and trouble with me, when someday I met open sourc community, just like a deer thirst for rivulet. since then when should implement something , I first to find wether wonderful open souce component exist, and take it and benefit, so very very thanks those great man who develop and open source! sometime I am ashamed because only continuous take what I needed but nothing has been done for you, so I have a mind to fix it when early 2018. Unforunatly, I am lazy, firstly make a greatful plan but now you see just a bullshit, however it's my first trying, so please forgive me more ^_^, I will  better to do in the futrue!
 

## What Can do
This project main to provider a lightweight RPC be based on json, my company's busnisess just focus on low concurrency scenarios, so what should consider is easy to integrate and easy to understand how to use it, with this mind, i found some famous componts like gRPC、 Thrift, these very heavyweight and why should have two channel to communicat with each other, maybe should deep into can know why. i want to build a RPC with myself, it draw lessons from WCF, i plan define an object interface who only has two method: serialize and deserialize, these method will help us to convert user object to json string, and RPC will send or recevie json string by tcp, these only need one channel just can statisfied demand for communicat each other, and will don't consider any protocol, what we just to do is define class which as an parameter need by a rpc requst or response needed! my plan just these, but now what you can see, i called 0.1 version, which only impelement transfer json string between client and server, and define an base class RPCClient, RPCServer,which you should inherit and implement some method as Register etc. 

## Status
I has implement 0.2 version and using long time, fix some bugs, but very sorry that the code is on another computer, so now push is 0.1 version, i will push 0.2 version later, but the release 1.0 version very sorry that not planned in the next year, becasue something more interesting attracted me, for examle Real-time Data Acquisition Platform, Real-time Computing System and Master Data Storage, it maybe 
enthusiasm sprang up on the spur of the moment, but how do you know the result before the end? so let's wait and see.

                                                                                                                      zlp 
                                                                                                                      2018-12-31 15:19:00                                                
