
问题： 开头缺一个 header + 一段只有音频的数据

```xml
Script Tag 略
<Tag Type="Audio" Flag="Header" Size="7" Timestamp="0">
  <BinaryData>AF00119056E500</BinaryData>
</Tag>
<Tag Type="Audio" Flag="Header" Size="7" Timestamp="0">
  <BinaryData>AF00119056E500</BinaryData>
</Tag>
<Tag Type="Audio" Flag="None" Size="8" Timestamp="0" />
...
全 Audio Data
...
<Tag Type="Video" Flag="Header Keyframe" Size="59" Timestamp="8011">
  <BinaryData>170000000001640028FFE1002767640028AC2CA501E0089F97016A020202800001F40000753070000016E36000016E360DDE5C1401000468EB8F2C</BinaryData>
</Tag>
<Tag Type="Video" Flag="Keyframe" Size="231930" Timestamp="8045">
  <Nalus StartPosition="9" FullSize="12" Type="Sei" />
  <Nalus StartPosition="25" FullSize="8" Type="Sei" />
  <Nalus StartPosition="37" FullSize="231893" Type="CodedSliceOfAnIdrPicture" />
</Tag>
...
正常数据
```
