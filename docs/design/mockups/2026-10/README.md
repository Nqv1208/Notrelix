# Notrelix mockups, October 2026 (reference copy)

Source canvas: https://claude.ai/artifact/18L4jUXGfdXh7Wn9YQvA2x, version 46.

**Reference only. Product docs and code are authoritative; these files never ship.** They exist so the built UI can be compared with the approved design. Mockup world data (Minh An Group, Nguyễn Thu Hà, ...) may appear only in `src/verification/*` fixtures and stories of the product, never in product code.

## Layout

```text
README.md        this index
render.py        renders one screen state to static HTML
decisions.md     shared review decisions from the design review
briefs/          the design briefs the screens were produced from
  mobile.md  tablet.md  group4.md  group4-rules.md
src/             the 43 Final-*.dc.html screen sources, copied unchanged
```

## Render a state

`render.py` resolves `src/` next to itself, so it runs from any directory. It needs only the Python standard library and `node`.

```bash
python3 -I docs/design/mockups/2026-10/render.py Final-Board.dc.html /tmp/board.html state=chi-xem mode=sang nav=an
open /tmp/board.html
```

Arguments: `<File.dc.html> <out.html> key=value ...`. Keys are the props listed in the table below. A prop you omit takes the default declared in the file, which is always the first value listed below (for example `Final-Table` defaults to `state=chon-nhieu`). `mode` is `sang` (light) or `dem` (dark) in every file; the table omits it. `nav` is the Notrelix system rail (`an` hidden, `hien-de` overlay, `hien-day` pinned); `sidebar` is the tablet sidebar (`mo` open, `thu-gon` collapsed).

## Screen index

Device comes from the file's preview size. Phase and owner come from the roadmap; `P17+` is tablet then native mobile, built after the web screens, and the owner column then names the responsive or native owner with the web owner in brackets.

| File | Device | Props and values | Roadmap phase | Owner package |
|---|---|---|---|---|
| `Final-Account.dc.html` | Desktop 1440 | `state`: `giao-dien`, `giao-dien-san-ho`, `ho-so`, `bao-mat`, `thong-bao`, `workspace`; `nav`: `an`, `hien-de`, `hien-day` | P12 | features/account |
| `Final-Auth.dc.html` | Desktop 1440 | `state`: `email`, `mat-khau`, `sso`, `xac-minh`, `loi`, `quen-mat-khau`, `da-gui`, `loi-moi`, `het-han` | P12 | features/auth |
| `Final-Automation.dc.html` | Desktop 1440 | `state`: `soan`, `chon-su-kien`, `chon-hanh-dong`, `lich`, `sua-khi-dang-bat`, `loi-cau-hinh`, `nhat-ky`, `lan-chay-loi`; `nav`: `an`, `hien-de`, `hien-day` | P13 | product/automation |
| `Final-Billing.dc.html` | Desktop 1440 | `state`: `tong-quan`, `so-sanh-goi`, `mua-cho`, `dang-xu-ly`, `hoa-don`, `qua-han`, `ha-goi`, `het-cho`, `chi-xem`; `nav`: `an`, `hien-de`, `hien-day` | P15 | features/billing |
| `Final-Board.dc.html` | Desktop 1440 | `state`: `binh-thuong`, `chi-xem`, `khong-ket-qua`; `detail`: `side`, `center`, `dong`; `nav`: `an`, `hien-de`, `hien-day` | P2 | product/work-management/web |
| `Final-Calendar.dc.html` | Desktop 1440 | `state`: `keo-tha`, `tao-nhanh`, `binh-thuong`, `khong-ket-qua`; `range`: `thang`, `tuan`, `ngay`; `nav`: `an`, `hien-de`, `hien-day` | P5 | product/work-management/web |
| `Final-Command.dc.html` | Desktop 1440 | `state`: `tim-kiem`, `chua-go`, `lenh`, `khong-thay`, `dang-chay`; `nav`: `an`, `hien-de`, `hien-day` | P7 | features/workspace shell + features/search |
| `Final-Detail.dc.html` | Desktop 1440 | `state`: `tong-quan`, `lich-su`, `xung-dot`, `chi-xem`, `binh-luan`; `nav`: `an`, `hien-de`, `hien-day` | P3 | product/work-management/web + apps/web route |
| `Final-Docs.dc.html` | Desktop 1440 | `state`: `binh-thuong`, `menu-tep`, `go-lenh`, `nhac-viec`, `binh-luan`, `muc-luc`, `loi-luu`, `chia-se`, `phien-ban`; `nav`: `an`, `hien-de`, `hien-day` | P11 | product/docs/web |
| `Final-Home.dc.html` | Desktop 1440 | `state`: `binh-thuong`, `xong-viec`, `dang-tai`; `nav`: `an`, `hien-de`, `hien-day` | P6 | features/workspace |
| `Final-Inbox.dc.html` | Desktop 1440 | `state`: `binh-thuong`, `het-viec`, `tat-ca`; `nav`: `an`, `hien-de`, `hien-day` | P6 | features/notifications |
| `Final-Integrations.dc.html` | Desktop 1440 | `state`: `danh-sach`, `chi-tiet`, `uy-quyen-lai`, `webhook`, `ket-noi`, `ngat-ket-noi`, `can-nang-cap`, `khong-co-quyen`; `nav`: `an`, `hien-de`, `hien-day` | P15 | features/integrations |
| `Final-Members.dc.html` | Desktop 1440 | `state`: `danh-sach`, `doi-vai-tro`, `them-nguoi`, `quan-tri-cuoi`, `go-nhom`; `nav`: `an`, `hien-de`, `hien-day` | P8 | features/workspace |
| `Final-MyWork.dc.html` | Desktop 1440 | `state`: `tat-ca`, `theo-board`, `chon-nhieu`, `da-xong`, `trong`, `chi-tiet`, `chon-nhom`, `doi-trang-thai`, `them-viec`; `nav`: `an`, `hien-de`, `hien-day` | P6 | features/workspace |
| `Final-Onboarding.dc.html` | Desktop 1440 | `state`: `tao-workspace`, `dia-chi-trung`, `phong-ban`, `moi-nguoi`, `mau-board`, `xong`, `tham-gia` | P12 | features/workspace |
| `Final-Reports.dc.html` | Desktop 1440 | `state`: `tong-quan`, `danh-sach`, `sua-bo-cuc`, `them-widget`, `khoan-sau`, `mot-phan`, `xuat`, `trong`; `nav`: `an`, `hien-de`, `hien-day` | P14 | to be decided (product/analytics or features) |
| `Final-Settings.dc.html` | Desktop 1440 | `state`: `truong`, `them-truong`, `luu-tru`, `che-do-xem`, `xem-loi`, `khong-co-quyen`, `chung`, `nhom-viec`, `tu-dong`; `nav`: `an`, `hien-de`, `hien-day` | P9 | product/work-management/web |
| `Final-Share.dc.html` | Desktop 1440 | `state`: `chia-se`, `doi-quyen`, `rieng-tu`, `nhat-ky` | P8 | features/governance |
| `Final-Space.dc.html` | Desktop 1440 | `state`: `tong-quan`, `tao-moi`, `phong-moi`, `thiet-lap`, `chi-xem`; `nav`: `an`, `hien-de`, `hien-day` | P8 | features/workspace |
| `Final-States.dc.html` | Desktop 1440 | (none; `mode` only) | P16 | all |
| `Final-Table.dc.html` | Desktop 1440 | `state`: `chon-nhieu`, `sua-o`, `binh-thuong`, `khong-ket-qua`, `da-luu-tru`; `density`: `thoai-mai`, `gon`; `nav`: `an`, `hien-de`, `hien-day` | P4 | product/work-management/web |
| `Final-Teams.dc.html` | Desktop 1440 | `state`: `danh-sach`, `chi-tiet`, `tao-moi`, `luu-tru`, `chia-se`, `khong-co-quyen`; `nav`: `an`, `hien-de`, `hien-day` | P10 | features/workspace |
| `Final-Timeline.dc.html` | Desktop 1440 | `state`: `binh-thuong`, `keo-gian`, `duong-gang`; `scale`: `ngay`, `tuan`, `thang`; `nav`: `an`, `hien-de`, `hien-day` | P5 | product/work-management/web |
| `Final-T-Board.dc.html` | Tablet 1180 | `state`: `binh-thuong`, `chia-doi`, `keo-the`, `menu-the`, `chi-xem`, `doi-workspace`; `sidebar`: `mo`, `thu-gon` | P17+ | wm-web responsive (web owner: product/work-management/web) |
| `Final-T-Calendar.dc.html` | Tablet 1180 | `state`: `tuan`, `thang`, `chia-doi`, `dat-han`, `keo-tha`; `sidebar`: `mo`, `thu-gon` | P17+ | wm-web responsive (web owner: product/work-management/web) |
| `Final-T-Detail.dc.html` | Tablet 1180 | `state`: `tong-quan`, `doi-nguoi`, `nhac-ten`, `xung-dot`, `chi-xem`, `lich-su`; `sidebar`: `thu-gon`, `mo` | P17+ | wm-web responsive (web owner: product/work-management/web + apps/web route) |
| `Final-T-MyWork.dc.html` | Tablet 1180 | `state`: `tat-ca`, `chia-doi`, `vuot`, `da-xong`, `trong`, `them-viec`; `sidebar`: `mo`, `thu-gon` | P17+ | wm-web responsive (web owner: features/workspace) |
| `Final-T-Table.dc.html` | Tablet 1180 | `state`: `binh-thuong`, `chia-doi`, `sua-o`, `chon-nhieu`, `khong-ket-qua`; `sidebar`: `mo`, `thu-gon` | P17+ | wm-web responsive (web owner: product/work-management/web) |
| `Final-M-Account.dc.html` | Mobile 390 | `state`: `tong-quan`, `giao-dien`, `giao-dien-dem`, `bao-mat`, `thong-bao` | P17+ | apps/mobile + product/*/mobile (web owner: features/account) |
| `Final-M-Auth.dc.html` | Mobile 390 | `state`: `chao`, `email`, `sso`, `xac-minh`, `loi`, `loi-moi`, `vao-workspace` | P17+ | apps/mobile + product/*/mobile (web owner: features/auth) |
| `Final-M-Automation.dc.html` | Mobile 390 | `state`: `danh-sach`, `chi-tiet`, `soan`, `nhat-ky`, `lan-chay-loi`, `tat-quy-tac` | P17+ | apps/mobile + product/*/mobile (web owner: product/automation) |
| `Final-M-Board.dc.html` | Mobile 390 | `state`: `kanban`, `danh-sach`, `chuyen-cot`, `loc`, `them-viec`, `chi-xem`, `menu-board` | P17+ | apps/mobile + product/*/mobile (web owner: product/work-management/web) |
| `Final-M-Calendar.dc.html` | Mobile 390 | `state`: `tuan`, `thang`, `ngay`, `chua-co-ngay`, `doi-han` | P17+ | apps/mobile + product/*/mobile (web owner: product/work-management/web) |
| `Final-M-Detail.dc.html` | Mobile 390 | `state`: `tong-quan`, `doi-trang-thai`, `giao-viec`, `nhac-ten`, `xung-dot`, `chi-xem`, `ngoai-tuyen`, `cuon-viec-con`, `cuon-lien-ket`, `thao-tac` | P17+ | apps/mobile + product/*/mobile (web owner: product/work-management/web + apps/web route) |
| `Final-M-Docs.dc.html` | Mobile 390 | `state`: `doc`, `sua`, `binh-luan`, `muc-luc`, `loi-luu`, `chi-xem`, `thao-tac` | P17+ | apps/mobile + product/*/mobile (web owner: product/docs/web) |
| `Final-M-Home.dc.html` | Mobile 390 | `state`: `binh-thuong`, `chuyen-workspace`, `vuot-viec`, `dang-tai`, `xong-viec` | P17+ | apps/mobile + product/*/mobile (web owner: features/workspace) |
| `Final-M-Inbox.dc.html` | Mobile 390 | `state`: `can-xu-ly`, `doc`, `vuot`, `nhac-lai`, `tat-ca`, `het-viec` | P17+ | apps/mobile + product/*/mobile (web owner: features/notifications) |
| `Final-M-Members.dc.html` | Mobile 390 | `state`: `danh-sach`, `doi-vai-tro`, `quan-tri-cuoi`, `them-nguoi`, `go-nhom`, `chi-xem` | P17+ | apps/mobile + product/*/mobile (web owner: features/workspace) |
| `Final-M-MyWork.dc.html` | Mobile 390 | `state`: `tat-ca`, `vuot`, `hoan`, `loc`, `da-xong`, `trong`, `them-viec` | P17+ | apps/mobile + product/*/mobile (web owner: features/workspace) |
| `Final-M-Reports.dc.html` | Mobile 390 | `state`: `tong-quan`, `danh-sach`, `chi-tiet-widget`, `bo-loc`, `sap-xep`, `mot-phan` | P17+ | apps/mobile + product/*/mobile (web owner: to be decided, see Final-Reports) |
| `Final-M-Search.dc.html` | Mobile 390 | `state`: `gan-day`, `ket-qua`, `khong-co`, `nguoi`, `dang-tim` | P17+ | apps/mobile + product/*/mobile (web owner: features/search) |
| `Final-M-Share.dc.html` | Mobile 390 | `state`: `chia-se`, `doi-quyen`, `rieng-tu`, `nhat-ky`, `da-sao-chep` | P17+ | apps/mobile + product/*/mobile (web owner: features/governance) |
| `Final-M-Space.dc.html` | Mobile 390 | `state`: `danh-sach`, `phong`, `tao-moi`, `chi-xem`, `thiet-lap` | P17+ | apps/mobile + product/*/mobile (web owner: features/workspace) |

## Related

- [`decisions.md`](decisions.md): decisions from the design review that apply across screens.
- [`briefs/`](briefs/): the mobile, tablet and group 4 briefs and the group 4 product rules the screens follow.
