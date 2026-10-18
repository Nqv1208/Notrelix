/**
 * Deterministic scenarios of the document page for stories and interaction
 * tests. The data is the Final-Docs mockup world (Minh An Group, the invoice
 * process page); it lives here only, never in product code.
 */
import { docPageCopy } from "../components/doc-page.copy";
import type {
  DocBlockView,
  DocCommentView,
  DocHeaderView,
  DocPerson,
  DocRelatedDoc,
  DocShareView,
  DocSpaceView,
  DocTaskRef,
  DocTaskResolution,
  DocTocEntry,
  DocVersionView,
} from "../components/doc-page-models";
import type { DocPageScreenSurfaceProps } from "../components/doc-page-screen-surface";

export const docsCopy = docPageCopy("vi");

export const KL: DocPerson = {
  id: "user-kl",
  name: "Đỗ Khánh Linh",
  initials: "KL",
  tone: "gold",
};
export const LT: DocPerson = {
  id: "user-lt",
  name: "Lý Thu Trang",
  initials: "LT",
  tone: "crimson",
};
export const HA: DocPerson = {
  id: "user-ha",
  name: "Nguyễn Thu Hà",
  initials: "HA",
  tone: "sage",
};
const MQ: DocPerson = {
  id: "user-mq",
  name: "Trần Minh Quân",
  initials: "MQ",
  tone: "blue",
};
const HP: DocPerson = {
  id: "user-hp",
  name: "Lê Hoàng Phúc",
  initials: "HP",
  tone: "coral",
};
const NA: DocPerson = {
  id: "user-na",
  name: "Phan Ngọc Anh",
  initials: "NA",
  tone: "violet",
};

const task = (
  key: string,
  title: string,
  status: DocTaskRef["status"],
  assignee: DocPerson | null,
  due: DocTaskRef["due"],
): DocTaskRef => ({
  id: `task-${key.toLowerCase()}`,
  key,
  title,
  status,
  assignee,
  due,
  href: "#",
});

export const TASK_144 = task(
  "ERP-144",
  "Kiểm thử phân hệ công nợ với số liệu tháng 9",
  "doing",
  HP,
  { kind: "today" },
);
export const TASK_142 = task(
  "ERP-142",
  "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
  "doing",
  MQ,
  { kind: "overdue", days: 2 },
);
export const TASK_151 = task(
  "ERP-151",
  "Cấu hình quyền truy cập theo phòng ban cho phân hệ Kế toán",
  "todo",
  NA,
  { kind: "date", label: "18/10" },
);
export const TASK_149 = task(
  "ERP-149",
  "Viết tài liệu quy trình nhập hoá đơn đầu vào",
  "todo",
  KL,
  { kind: "date", label: "25/10" },
);
export const TASK_129 = task(
  "ERP-129",
  "Chốt danh mục tài khoản kế toán dùng chung",
  "done",
  KL,
  { kind: "done", label: "30/09" },
);
export const TASK_PRIVATE: DocTaskResolution = {
  id: "task-private",
  restricted: true,
};

export const SPACE: DocSpaceView = {
  id: "space-kt",
  name: "Phòng Kế toán",
  mark: "KT",
  tone: "gold",
  visibility: "private",
};

export const HEADER: DocHeaderView = {
  title: "Quy trình nhập hoá đơn đầu vào",
  crumbs: ["Kế toán", "Quy trình", "Nhập hoá đơn đầu vào"],
  status: "review",
  editedBy: "Đỗ Khánh Linh",
  editedAt: "16:40 hôm qua",
  presence: [KL, LT],
};

export const BLOCKS: readonly DocBlockView[] = [
  { id: "b1", kind: "heading", level: 2, text: "Phạm vi áp dụng" },
  {
    id: "b2",
    kind: "paragraph",
    type: "paragraph",
    segments: [
      {
        kind: "text",
        text: "Quy trình áp dụng cho mọi hoá đơn điện tử đầu vào của Minh An Group từ ngày 01/11/2026, khi phân hệ công nợ của ERP chính thức vận hành. Việc kiểm thử phân hệ đang theo dõi tại ",
      },
      { kind: "task", task: TASK_144 },
      { kind: "text", text: "." },
    ],
  },
  { id: "b3", kind: "heading", level: 2, text: "Các bước xử lý" },
  {
    id: "b4",
    kind: "numbered",
    type: "numbered_list",
    segments: [
      {
        kind: "text",
        text: "Hộp thư hoá đơn tự động chuyển hoá đơn vào hàng chờ của nhóm Công nợ.",
      },
    ],
  },
  {
    id: "b5",
    kind: "numbered",
    type: "numbered_list",
    segments: [
      {
        kind: "text",
        text: "Kế toán viên đối chiếu với đơn đặt hàng và phiếu nhập kho trong tối đa 2 ngày làm việc.",
      },
    ],
  },
  {
    id: "b6",
    kind: "numbered",
    type: "numbered_list",
    segments: [
      {
        kind: "text",
        text: "Hoá đơn sai lệch quá 0,5% chuyển cho trưởng nhóm duyệt.",
      },
    ],
  },
  {
    id: "b7",
    kind: "numbered",
    type: "numbered_list",
    segments: [
      {
        kind: "text",
        text: "Hạch toán vào ERP và gắn mã chi phí theo phòng ban, theo phân quyền ở ",
      },
      { kind: "task", task: TASK_151 },
      { kind: "text", text: "." },
    ],
  },
  { id: "b8", kind: "heading", level: 2, text: "Chuẩn bị trước khi áp dụng" },
  {
    id: "b9",
    kind: "todo",
    text: "Chốt danh mục tài khoản dùng chung",
    checked: true,
  },
  {
    id: "b10",
    kind: "todo",
    text: "Đào tạo nhóm Công nợ về đối chiếu tự động",
    checked: false,
  },
  {
    id: "b11",
    kind: "todo",
    text: "Thông báo 42 nhà cung cấp về hộp thư hoá đơn mới",
    checked: false,
  },
  { id: "b12", kind: "heading", level: 2, text: "Việc liên quan" },
  {
    id: "b13",
    kind: "liveTable",
    boardName: "Triển khai ERP Q4",
    filterLabel: "Phân hệ Kế toán · chưa xong",
    boardHref: "#",
    tasks: [TASK_142, TASK_144, TASK_151, TASK_149, TASK_PRIVATE],
  },
  {
    id: "b14",
    kind: "callout",
    text: "Hoá đơn từ 20 triệu đồng phải có chứng từ thanh toán không dùng tiền mặt mới được khấu trừ thuế GTGT.",
  },
];

export const TASKS: readonly DocTaskResolution[] = [
  TASK_144,
  TASK_142,
  TASK_151,
  TASK_129,
  TASK_PRIVATE,
];

export const RELATED_DOCS: readonly DocRelatedDoc[] = [
  {
    id: "rel-1",
    kind: "doc",
    title: "Đặc tả triển khai ERP",
    detail: "Tài liệu · Công nghệ",
    href: "#",
  },
  {
    id: "rel-2",
    kind: "task",
    title: "ERP-149 Viết tài liệu quy trình nhập hoá đơn đầu vào",
    detail: "Công việc · đính kèm tài liệu này",
    href: "#",
  },
];

export const TOC: readonly DocTocEntry[] = [
  { id: "title", level: 1, text: "Quy trình nhập hoá đơn đầu vào" },
  { id: "b1", level: 2, text: "Phạm vi áp dụng" },
  { id: "b3", level: 2, text: "Các bước xử lý", active: true },
  { id: "b8", level: 2, text: "Chuẩn bị trước khi áp dụng" },
  { id: "b12", level: 2, text: "Việc liên quan" },
];

export const COMMENTS: readonly DocCommentView[] = [
  {
    id: "c1",
    author: LT,
    quote: "trong tối đa 2 ngày làm việc",
    body: "Cuối tháng lượng hoá đơn gấp ba, đề xuất 3 ngày cho tuần cuối tháng.",
    when: "Hôm qua",
  },
  {
    id: "c2",
    author: KL,
    quote: null,
    body: "Đồng ý, mình sẽ thêm ngoại lệ. Anh Nam rà lại quy định thuế giúp nhé.",
    when: "Hôm qua",
  },
];

export const VERSIONS: readonly DocVersionView[] = [
  {
    id: "v1",
    author: KL,
    time: "16:40",
    day: "Hôm qua",
    stamp: "11/10 16:40",
    summary: "Đổi trạng thái Đang soạn → Chờ duyệt",
    current: true,
  },
  {
    id: "v2",
    author: KL,
    time: "15:05",
    day: "Hôm qua",
    stamp: "11/10 15:05",
    summary: "Sửa bước 3 · thêm 2 dòng danh sách kiểm",
  },
  {
    id: "v3",
    author: LT,
    time: "15:42",
    day: "10/10",
    stamp: "10/10 15:42",
    summary: "Thêm lưu ý thuế GTGT cho hoá đơn từ 20 triệu đồng",
  },
  {
    id: "v4",
    author: KL,
    time: "11:20",
    day: "10/10",
    stamp: "10/10 11:20",
    name: "Bản trình Ban giám đốc",
    summary: "Thêm mục Việc liên quan",
  },
  {
    id: "v5",
    author: KL,
    time: "17:05",
    day: "08/10",
    stamp: "08/10 17:05",
    summary: "Khôi phục từ phiên bản 05/10 14:20",
  },
  {
    id: "v6",
    author: LT,
    time: "09:40",
    day: "08/10",
    stamp: "08/10 09:40",
    summary: "Viết lại mục 2 · bỏ bước duyệt hai cấp",
  },
  {
    id: "v7",
    author: KL,
    time: "14:20",
    day: "05/10",
    stamp: "05/10 14:20",
    summary: "Tạo trang",
  },
];

export const SHARE: DocShareView = {
  space: SPACE,
  spaceMemberCount: 11,
  members: [
    {
      id: "m-kl",
      kind: "person",
      person: KL,
      name: "Đỗ Khánh Linh",
      detail: "Quản trị Phòng Kế toán",
      creator: true,
      level: "manage",
      fixed: true,
    },
    {
      id: "m-ha",
      kind: "person",
      person: HA,
      name: "Nguyễn Thu Hà",
      detail: "Quản trị workspace · được thêm riêng",
      you: true,
      level: "edit",
    },
    {
      id: "m-team",
      kind: "team",
      name: "Nhóm Kỹ sư ERP",
      detail: "Nhóm workspace · 4 người · ngoài phòng Kế toán",
      level: "comment",
    },
    {
      id: "m-gb",
      kind: "person",
      person: {
        id: "user-gb",
        name: "Lâm Gia Bảo",
        initials: "GB",
        tone: "sage",
      },
      name: "Lâm Gia Bảo",
      detail:
        "baolg@kiemtoan-an.vn · chỉ thấy trang này · hết quyền 30/11/2026",
      outside: true,
      level: "view",
    },
  ],
};

const MENTION_TASKS: readonly DocTaskRef[] = [
  TASK_142,
  TASK_144,
  task(
    "ERP-147",
    "Chuẩn bị dữ liệu mẫu cho buổi đào tạo người dùng",
    "todo",
    null,
    null,
  ),
  TASK_149,
];

/** The page in its normal state (Final-Docs `binh-thuong`). */
export function docsPageScreenDefaultScenario(): DocPageScreenSurfaceProps {
  return {
    copy: docsCopy,
    header: HEADER,
    saveState: "saved",
    chips: [
      {
        id: "scope",
        kind: "scope",
        label: "Phòng Kế toán · Riêng tư",
      },
      { id: "date", kind: "date", label: "Áp dụng từ 01/11/2026" },
      { id: "board", kind: "board", label: "Triển khai ERP Q4", href: "#" },
    ],
    why: "Bạn thấy trang của phòng Kế toán vì bạn là quản trị workspace.",
    blocks: BLOCKS,
    tasks: TASKS,
    relatedDocs: RELATED_DOCS,
    toc: TOC.map((entry) => ({ ...entry, active: undefined })),
    comments: COMMENTS,
    panelTab: "links",
    overlay: { kind: "none" },
    mentionTasks: [],
    toolbar: {
      blockType: "heading_2",
      fontSize: 20,
      bold: true,
      italic: false,
      underline: false,
      strike: false,
      align: "left",
    },
    canComment: true,
    canFormat: true,
  };
}

export function docsPageScreenLoadingScenario(): DocPageScreenSurfaceProps {
  return { ...docsPageScreenDefaultScenario(), status: "loading" };
}

export function docsPageScreenNotFoundScenario(): DocPageScreenSurfaceProps {
  return { ...docsPageScreenDefaultScenario(), status: "notFound" };
}

export function docsPageScreenEmptyScenario(): DocPageScreenSurfaceProps {
  return {
    ...docsPageScreenDefaultScenario(),
    header: { ...HEADER, title: "Tài liệu chưa đặt tên", presence: [] },
    chips: [],
    why: null,
    blocks: [],
    tasks: [],
    relatedDocs: [],
    toc: [{ id: "title", level: 1, text: "Tài liệu chưa đặt tên" }],
    comments: [],
  };
}

export function docsPageScreenReadOnlyScenario(): DocPageScreenSurfaceProps {
  return {
    ...docsPageScreenDefaultScenario(),
    readOnly: true,
    canFormat: false,
    canComment: false,
  };
}

export function docsPageScreenSaveErrorScenario(): DocPageScreenSurfaceProps {
  return { ...docsPageScreenDefaultScenario(), saveState: "error" };
}

export function docsPageScreenCommentsScenario(): DocPageScreenSurfaceProps {
  const base = docsPageScreenDefaultScenario();
  return {
    ...base,
    panelTab: "comments",
    blocks: BLOCKS.map((block) =>
      block.id === "b5" && block.kind === "numbered"
        ? {
            ...block,
            segments: [
              {
                kind: "text" as const,
                text: "Kế toán viên đối chiếu với đơn đặt hàng và phiếu nhập kho ",
              },
              { kind: "mark" as const, text: "trong tối đa 2 ngày làm việc" },
              { kind: "text" as const, text: "." },
            ],
          }
        : block,
    ),
  };
}

export function docsPageScreenContentsScenario(): DocPageScreenSurfaceProps {
  return {
    ...docsPageScreenDefaultScenario(),
    panelTab: "toc",
    toc: TOC,
  };
}

export function docsPageScreenFileMenuScenario(): DocPageScreenSurfaceProps {
  return {
    ...docsPageScreenDefaultScenario(),
    overlay: { kind: "fileMenu", downloadOpen: true },
  };
}

export function docsPageScreenSlashScenario(): DocPageScreenSurfaceProps {
  const base = docsPageScreenDefaultScenario();
  const blocks: DocBlockView[] = [];
  for (const block of BLOCKS) {
    blocks.push(block);
    if (block.id === "b7") {
      blocks.push({
        id: "b-slash",
        kind: "paragraph",
        type: "paragraph",
        segments: [{ kind: "text", text: "/danh" }],
      });
    }
  }
  return {
    ...base,
    blocks,
    overlay: {
      kind: "slash",
      blockId: "b-slash",
      query: "danh",
      text: "/danh",
      before: "/danh",
    },
  };
}

export function docsPageScreenMentionScenario(): DocPageScreenSurfaceProps {
  const base = docsPageScreenDefaultScenario();
  const text = "Quyền xem hoá đơn theo phòng ban được cấu hình ở @ERP-14";
  const blocks: DocBlockView[] = [];
  for (const block of BLOCKS) {
    blocks.push(block);
    if (block.id === "b2") {
      blocks.push({
        id: "b-mention",
        kind: "paragraph",
        type: "paragraph",
        segments: [{ kind: "text", text }],
      });
    }
  }
  return {
    ...base,
    blocks,
    mentionTasks: MENTION_TASKS,
    overlay: {
      kind: "mention",
      blockId: "b-mention",
      query: "ERP-14",
      text,
      before: text,
    },
  };
}

export function docsPageScreenShareScenario(): DocPageScreenSurfaceProps {
  return {
    ...docsPageScreenDefaultScenario(),
    overlay: { kind: "share" },
    share: SHARE,
  };
}

export function docsPageScreenVersionsScenario(): DocPageScreenSurfaceProps {
  const base = docsPageScreenDefaultScenario();
  return {
    ...base,
    toolbar: undefined,
    blocks: BLOCKS.map((block) => {
      if (block.id === "b6" && block.kind === "numbered") {
        return {
          ...block,
          segments: [
            { kind: "text" as const, text: "Hoá đơn sai lệch quá " },
            { kind: "del" as const, text: "1%" },
            { kind: "text" as const, text: " " },
            { kind: "ins" as const, text: "0,5%" },
            { kind: "text" as const, text: " chuyển cho " },
            { kind: "del" as const, text: "kế toán trưởng" },
            { kind: "text" as const, text: " " },
            { kind: "ins" as const, text: "trưởng nhóm" },
            { kind: "text" as const, text: " duyệt." },
          ],
        };
      }
      if ((block.id === "b10" || block.id === "b11") && block.kind === "todo") {
        return { ...block, inserted: true };
      }
      return block;
    }),
    versions: {
      list: VERSIONS,
      selectedId: "v3",
      namedOnly: false,
    },
  };
}
