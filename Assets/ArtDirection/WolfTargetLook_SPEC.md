
# Wolf Target Look Texture Spec

## Визуальная цель

Пакет ориентирован на modern Wolf3D-style комнату:

- тёмно-синие крупные стеновые панели;
- глубокие чёрные металлические швы;
- бирюзовая металлическая дверь;
- тёмная heavy frame, заклёпки, access panel;
- серый плиточный пол;
- тёмный панельный потолок;
- металлические trims.

## Отличие от старых Wolf-текстур

Оригинальные Wolf-текстуры в reference-проекте используются как atlas/sprite-style source. Для Unity URP нужен PBR-подход:

old atlas texture → albedo + height + normal + roughness + AO + metallic/smoothness

## Материалы

### BlueWall_Target

- tileable
- тёмный синий цвет
- крупные блоки
- швы тёмные
- normal map даёт глубину панелей
- AO затемняет швы
- roughness не равномерный

### DoorTeal_Target

- non-tileable
- цельная дверь
- тёмная рама
- заклёпки
- access panel
- yellow handle/detail
- metallic map

### DarkMetalTrim_Target

Для геометрических seams/frames. Его лучше применять на отдельные тонкие mesh-элементы:
- wall seams
- baseboards
- door frame
- corner trims

## Самая важная рекомендация

Не пытайся сделать всё одной текстурой. Для сходства с target screenshot используй:
- BlueWall_Target на плоскость стены;
- DarkMetalTrim_Target на отдельные швы/рамы;
- DoorTeal_Target на дверь;
- отдельную геометрию для frame/rivets/access panel, если нужно ещё ближе к референсу.
