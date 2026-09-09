# LootRadius (SPT 4.1 포팅)

인벤토리를 열면 **주변 바닥의 아이템들이 오른쪽 패널에 모여서** 뜹니다. 하나씩 주우려고
바닥을 쳐다볼 필요가 없어집니다.

> **원작자 · 원본**
> **DrakiaXYZ** — https://github.com/DrakiaXYZ/SPT-LootRadius
>
> 이 저장소는 위 원작의 **포크**입니다. 기능은 그대로고, **SPT 4.1에서 빌드·동작하도록
> 포팅**한 것이 전부입니다.

## 쓰는 법

레이드 중 인벤토리(`Tab`)를 열면 오른쪽에 **Nearby Items** 패널이 생깁니다. 반경 안의
바닥 아이템이 거기 담기고, Ctrl+클릭이나 드래그로 가져오면 됩니다. 인벤토리를 닫으면
안 가져간 것들은 원래 자리로 돌아갑니다.

**F12 → `1. General` → `Loot Radius`** 로 반경을 조절합니다 (기본 2m, 0~10m).

퀘스트 아이템은 이 패널에서 드래그가 막혀 있습니다. 시야가 막힌 아이템도 제외되고요
(발밑 0.35m 안쪽은 예외 — 바닥에 살짝 파묻힌 것도 주울 수 있게).

## 설치

`DrakiaXYZ-LootRadius.dll` 을 `BepInEx\plugins\` 에 넣으면 끝입니다. 서버 모드는 없습니다.

## 4.1 포팅에서 바뀐 것

원본은 **SPT 3.11** 기준입니다 (`BepInDependency("com.SPT.core", "3.11.0")`). 3.11 → 4.1은
클라이언트 역난독화가 끼어 있어서, **이 모드가 쓰던 타입 이름이 하나도 안 남았습니다.**
전부 실제 4.1.5 `Assembly-CSharp.dll` 로 대조해서 다시 찾았습니다.

### 타입 · 멤버 이름

| 3.11 (난독화) | 4.1 (실제 이름) |
| --- | --- |
| `StashItemClass` | `EFT.InventoryLogic.Stash` |
| `StashGridClass` | `EFT.InventoryLogic.Grid` |
| `GClass2924` | `EFT.InventoryLogic.GridItemCollection` |
| `StashGridClass.GClass3784` | `Grid.NoFreeSpaceError` |
| `StashGridClass.GClass3785` | `Grid.ItemFiltersWontAllowError` |
| `StashGridClass.GClass3786` | `Grid.ItemNotInGridError` |
| `GClass3207` | `EFT.InventoryLogic.GridAddResult` |
| `GClass3205` | `EFT.InventoryLogic.ContainerRemoveResult` |
| `GStruct455<T>` | `Diz.LanguageExtensions.OperationResult<T>` |
| `GInterface381` | `IContainerResizeResult` |
| `GStruct395` | `NoContainerResizeResult` |
| `GStruct396` | `GridResizeResult` |
| `XYCellSizeStruct` | `IntVec2` |
| `GEventArgs3` | `RemoveItemEventArgs` |
| `GameWorld.GStruct126` | `GameWorld.ItemOwnerWorldData` |
| `TraderControllerClass` | `EFT.InventoryLogic.ItemController` |
| `ItemFactoryClass` | `EFT.ItemFactory` |
| `InteractionsHandlerClass` | `EFT.InventoryLogic.ItemManipulator` |
| `ItemContextAbstractClass` | `EFT.InventoryLogic.ItemContext` |
| `AddViewListClass` | `EFT.UI.UIParent` |
| `LayerMaskClass` | `LayersMaskController` |
| `method_9` | `Grid.PlaceItem` |
| `method_10` | `Grid.RemoveItem` |
| `dictionary_0` / `list_0` | `GridItemCollection.Items` / `.ItemsList` |
| `containedGridsView_0` | `SearchableItemView._containedGridsView` |
| `ItemUiContext` 의 이름 없는 `CompoundItem[]` 필드 | `_rightPanelItem` |
| `GridView` 의 이름 없는 `IItemOwner` 필드 | `_itemOwner` |

### 이름만이 아니라 구조가 바뀐 것 4가지

**1. `ItemsPanel.Close` 가 사라졌습니다.**
4.1의 `ItemsPanel`은 `Close`를 직접 선언하지 않습니다. 닫기는 상속받은
`UIElement.Close()` 가 처리하고, 그게 패널의 `UI` 목록을 Dispose 하면서 내부
`_wasClosed` 가 켜집니다. 그래서 패치 대상을 `UIElement.Close()` + `is ItemsPanel` 필터로
바꿨습니다.

컴파일러가 만든 `ItemsPanel.CG_method_1` 이 정확히 "패널이 닫혔다" 지점이라 더 좁게 걸 수
있었지만, `CG_` 이름은 역난독화가 붙이는 **인덱스 기반 이름**이라 빌드마다 움직입니다.
`UIElement.Close` 는 실존하는 공개 메서드라 그렇지 않습니다.

**2. `Stash` 의 그리드가 배열에서 단일 필드로 바뀌었습니다.**
3.11은 `stash.Grids = new StashGridClass[] { myGrid }` 하나로 끝났는데, 4.1의 `Stash` 는
`Stash.StashGrid` 타입의 `_grid` 필드를 따로 들고 있습니다. 게임 자체 생성자를 보면
`_grid` 와 `Grids[0]` 이 **같은 객체**를 가리킵니다:

```csharp
_grid = new StashGrid(template.Grids[0], this);
Grids = new Grid[1] { _grid };
```

그래서 커스텀 그리드를 `Grid` 가 아니라 **`Stash.StashGrid` 를 상속**하게 바꾸고, 양쪽에
같이 대입합니다. `Grids` 만 채우면 `_grid` 는 팩토리가 만든 원래 그리드에 남습니다.

**3. `GridView.OnItemRemoved` 가 명시적 인터페이스 구현이 됐습니다.**
`gridView.OnItemRemoved(...)` 로 직접 못 부릅니다. `IRemoveHandler` 로 캐스팅해야 합니다.

**4. UI 메서드 인자가 늘었습니다.**
`ItemsPanel.Show` 는 4개 → **14개**, `SimpleStashPanel.Show` 는 6개 → **8개**
(`sortingTable`, `searchAvailability` 추가). Harmony 는 이름으로 바인딩하니 postfix 는
필요한 인자만 받으면 되지만, `SimpleStashPanel.Show` 호출은 새 인자를 채워야 합니다.
바닥에서 주운 전리품은 이미 "수색된" 상태라 `EStashSearchAvailability.All` 을 넘깁니다.

### 그 외

- 구식 csproj → SDK 스타일, `net471` → `netstandard2.1`
- `..\..\` 상대 경로 제거. 이 경로는 저장소가 SPT 폴더 **안에 정확히 2단계**로 들어가 있어야만
  풀렸고, 다른 데 클론하면 모든 참조가 조용히 미해결이 됐습니다. 이제 `SptRoot` 기준이고,
  경로가 틀리면 이유를 말해주고 실패합니다
- `Properties/AssemblyInfo.cs` 삭제, 값은 csproj로 이전
- `SHA256Managed` (obsolete) → `SHA256.Create()`. **해시 결과가 같아서 기존 프로필의 스태시
  ID가 그대로 유지됩니다**
- `BepInDependency` 3.11.0 → 4.1.0

## 빌드

```
dotnet build SPT-LootRadius.csproj -c Release
```

`SptRoot` 기본값은 `E:\SPT 4.1` 입니다. 다르면 `-p:SptRoot="D:\내경로"`. 빌드하면
`BepInEx\plugins\` 로 자동 복사됩니다 (`-p:SkipDeploy=true` 로 끄고, Windows 외에서
강제하려면 `-p:OS=Windows_NT`).

## 검증

- **실제 4.1.5 `Assembly-CSharp.dll` 상대로 컴파일 — 경고 0**
- **42개 체크 하네스 전부 통과.** 컴파일러가 못 잡는 것들을 리플렉션으로 직접 확인했습니다:
  - `AccessTools.Field` 로 찾는 필드 3개가 실존하고 타입까지 맞는지 (이름이 틀리면 컴파일은
    되고 런타임에 `null` 이 됩니다)
  - Harmony 패치 대상 5개가 `GetMethod` 로 잡히는지
  - **`ItemsPanel.Show` 의 파라미터 이름 5개** — Harmony 는 이름으로 바인딩해서, 이름이
    바뀌면 인자가 조용히 안 들어옵니다
  - `ItemsPanel` 이 `Close` 를 선언하지 않고 `UIElement` 를 상속하는지 (패치 전략의 전제)
  - 오버라이드하는 `Grid` 멤버 4개가 실제로 virtual 인지
  - `Stash._grid` 의 타입이 `Stash.StashGrid` 인지 (커스텀 그리드의 상속 대상 근거)
  - `GridView.OnItemRemoved` 가 정말 직접 호출 불가인지
- **인게임 테스트는 아직 안 했습니다.**

## 라이선스

원작 DrakiaXYZ. `LICENSE.txt` 참고.
