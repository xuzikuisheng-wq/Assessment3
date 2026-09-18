# MyPacStudent

使用 Unity Hub 添加本文件夹，以 Unity 6000.4.11f1 打开。

首次打开时若根目录存在 BUILD_REQUESTED，工程会自动准备素材、构建并保存两个场景，然后依次运行主场景与动画展示场景的检查。请等待编译和约一分钟的 Play 模式检查完成，不要中途修改场景。

若已存在场景，构建工具不会覆盖它们。主场景为 Assets/Scenes/RecreatedLevel.unity，动画展示为 Assets/Scenes/AnimationLab.unity。

主场景按手册保留四象限地图。Play 时由生成器替换，主角按右、下、左、上四方向匀速移动，一圈约六秒。开场音乐在三秒以内切换到常规循环音乐。动画展示场景自动展示主角五种状态和每个敌人的十种状态。

菜单 Assessment 3 → Validate saved scenes and Play mode 可重新验证。结果写入 ValidationResults，Result.txt 为最终状态，MainPreview.png 与 AnimationLabPreview.png 为实际相机渲染图。若没有 Result.txt，则尚不能声称运行验证完成。

菜单 Assessment 3 → Build complete project scenes 仅用于尚未保存场景的首次构建。请先退出 Play 模式。

本项目使用提供的素材与手册示例。手工地图由编辑器工具按表自动放置并保存，不能将自动放置描述为个人手工制作过程。

学号：

当前工程不包含键盘控制、吃豆碰撞、敌人寻路和 Assessment 4 菜单；这些不属于给定手册本次要求。
