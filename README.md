2019.04.11 update
V1.2
- 支持自动清理跟踪日志
- 支持实时关键词筛选

V1.1
- 支持一键复制SQL语句
- 增加清除按钮，一键从库中清除所有普通查询数据

V1.0
#  简介
之前的工作一直使用的SQL SERVER, 用过的都知道，SQL SERVER有配套的SQL跟踪工具SQL Profiler，开发或者定位BUG过程中，可以在操作页面的时候，实时查看数据库执行的SQL语句，十分方便。最近的项目使用MySQL，没有类似的功能，感觉到十分的不爽，网上也没有找到合适的免费工具，所以自己研究做了一个简单工具。
###### 功能
- 实时查询MySql执行的SQL语句
- 查看性能异常的SQL（执行超过2秒）

###### 技术方案
- 前端vue，样式bootstrap
- 后台dotnet core mvc

先看一下的效果：
![](https://img2018.cnblogs.com/blog/540721/201812/540721-20181213175113602-615081202.png)



![](https://img2018.cnblogs.com/blog/540721/201812/540721-20181213175125506-1958892641.png)



# 实现原理
###### Mysql支持输出日志，通过以下命令查看当前状态

- show VARIABLES like '%general_log%' //是否开启输出所有日志

- show VARIABLES like '%slow_query_log%' //是否开启慢SQL日志
- show VARIABLES like '%log_output%' //查看日志输出方式（默认file，还支持table）
- show VARIABLES like '%long_query_time%' //查看多少秒定义为慢SQL


###### 下面我们将所有日志、慢SQL日志打开，日志输出修改为table，定义执行2秒以上的为慢SQL
- set global log_output='table' //日志输出到table（默认file）
- set global general_log=on;  //打开输出所有日志
- set global slow_query_log=on; //打开慢SQL日志
- set global long_query_time=2 //设置2秒以上为慢查询
- repair table mysql.general_log //修复日志表（如果general_log表报错的情况下执行）

注意：以上的设置，数据库重启后将失效，永久改变配置需要修改my.conf文件

###### 现在日志文件都存在数据库表里面了，剩下的工作就是取数并展示出来就行了。本项目后台使用的MVC取数，然后VUE动态绑定，Bootstrap渲染样式。
