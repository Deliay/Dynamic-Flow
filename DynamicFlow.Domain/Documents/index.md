Dynamic Flow
----
目标是做所有工作系统的前端，和所有一线人员做交互。

# 主要概念
本系统以树状卡片的的形式描述和展示工作流，辅以卡片上的标签进行权限、筛选和数据辅助。本文档将介绍下列部分：

1. 依赖关系抽象
2. 标签管理
3. 持久化层
4. 展示层
5. 任务筛选层
6. 外部系统接入
7. 权限管理的思考

## 依赖关系抽象
本系统以任务为核心构建有向无环任务依赖关系图（DAG），每个任务单元称为一个`Task`，展示任务的视图为`Tree`。同一个`Task`可能出现在多个`Tree`中。当`Task`状态或关联关系有变化时，会通知`Tree`。

每个`Task`都有自己的`ResolvePolicy`，当`Task`满足`ResolvePolicy`时，其状态才能被变更，典型场景就是正常的开发流程，需要产品先产出 `产品需求`，然后进行 `产品评审`，评审通过之后，才能`进入排期列表`，进入排期列表，等待开发进行`开发评估`之后，进入`开发流程`。

`ResolvePolicy`默认可选设置有`All`、`Or`、`Optional`。

- `All`: 需要所有上游`Task`都完成，才能开始这个`Task`。
- `Or`: 上游`Task`完成任一，均能开始这个`Task`。
- `Optional`: 该`Task`状态与上游任务无关

注意，`ResolvePolicy`是可以被扩展的，具体实现可以参照类`DefaultResolvePolicyFactory`的实现，上述默认策略实现也均在这个类中。

本系统定义了数个`Task`的生命周期，他们分别是`Locked`、`NotStart`、`InProgress`、`Paused`、`Completed`、`Failed`。下面将描述预期使用状态的语义：

- `Locked`：不满足`ResolvePolicy`，无法对任务进行任何操作
- `NotStart`: 满足`ResolvePolicy`，但还没有进行任何操作
- `InProgress`: 标记任务已经在进行中
- `Pause`: 标记任务状态为暂停，例如项目暂停或其他情形。
- `Completed`: 标记任务已经完成
- `Failed`: 标记任务失败

## 标签管理
本系统将任务相关的数据能力统一抽象到`Label`中，依赖关系默认实现中不会带`Label`相关功能，需要使用带标签的任务才能带上默认的标签管理的功能，抽象类为`LabeledTask<T>`。

标签是一个很好的将任务本身、任务依赖关系、三方隔离开的形式。

#### 标签元信息`LabelMetadata`
标签的元信息由`Namespace`和`Name`组成，标识这个`Label`从何而来，和最多允许的数量`AllowCount`。注意，标签的元信息是值类型，是自然存在的，只需要在需要的时候定义出来即可。

标签元信息的字符串描述为`{AllowCount}/{Namespace}/{Name}`

#### 标签`Label`
标签由`LabelMetadata`、`Id`和`Value`组成，其中`Value`是标签的值，`Id`是标签的唯一标识，两者均为字符串。


#### 标签传递
在标签的`dynflow`命名空间下，`Extends`、`Spread`具有特殊功能

- `Extends`: 在子`Task`标记一个`LabelMetadata`例如`0/dynflow/Extends`，从父`Task`查找这个标签，如果有的话就继承到子`Task`。
- `Spread`: 在父`Task`标记一个完整`LabelMetadata`，所有子`Task`均继承父`Task`上的`Label`。

#### 其他dynflow命名空间的标签及其理想用途
- `Id` 唯一标识
- `Name` 名称标识
- `Description` 任务描述
- `Due` 任务持续到
- `Duration` 任务经历时间
- `Assignee` 任务关联人
- `Overtime` 标记任务已超期
- `Group` 任务分组


### 理想中的标签使用方式

我们定义标签`dev-workflow`命名空间下的系列标签，并为`Task`贴上如下标签：
- `1/dev-workflow/Type = Development`
- `0/dynflow/Assignee = Joe`
- `0/dynflow/Extends = 1/dev-workflow/Project`
- (继承自父任务)`1/dev-workflow/Project = An epic project`

作为开发Joe，这样我们通过筛选 `0/dynflow/Assignee` 就能筛选出分配给 `Joe` 的所有任务，可以直接产出关于`Joe`的视图，方便我们的开发Joe快速定位分配给自己的任务。通过对`1/dnyflow/Due`的筛选，Joe可以很快的直到哪些任务临期。

再如，`Task`有如下标签
- `1/dev-workflow/Type = Product Iterate`
- `1/dynflow/Name = An epic feature`
- `0/dynflow/Assignee = Ben`
- `1/dev-workflow/Project = An epic project`

- `0/dynflow/Extends = 1/dev-workflow/Sprint`
- (继承自父任务)`1/dev-workflow/Sprint = 2023-12`

作为产品经理Ben，可以筛选 `1/dev-workflow/Project` 知道自己的项目中所有迭代的情况，也可以筛选`1/dev-workflow/Sprint = 2023-12`查看这个敏捷周期的所有任务状态。（备注，`Task`的依赖是不限制数量的。）

如上模式可以套用到项目规划、项目进展等多个场景中，不再赘述。


