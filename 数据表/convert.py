import pandas as pd
import os
def format_cell(value):
    if value is None:
        return ''
    return str(value)

def convert_excel_to_txt(folder_path='.'):
    # 计数器，用于最后汇总
    count = 0
    # 固定从脚本所在目录读取，避免受运行时工作目录影响
    script_dir = os.path.dirname(os.path.abspath(__file__))
    scan_dir = script_dir if folder_path in ('.', '') else os.path.abspath(folder_path)
    target_dir = os.path.join(os.path.dirname(__file__), '../Assets/GameMain/DataTables')
    target_dir = os.path.abspath(target_dir)
    
    # 确保目标目录存在
    os.makedirs(target_dir, exist_ok=True)
    
    for root, _, files in os.walk(scan_dir):
        for file_name in files:
            # 跳过 Excel 打开时生成的临时锁文件
            if file_name.startswith('~$'):
                continue
            if file_name.endswith(('.xlsx', '.xls')):
                file_path = os.path.join(root, file_name)
                base_name = os.path.splitext(file_name)[0]
                output_file = os.path.join(target_dir, f"{base_name}.txt")
                
                print(f"正在处理: {file_path}...")
                
                try:
                    # 读取 Excel
                    df = pd.read_excel(
                        file_path,
                        header=None,
                        keep_default_na=False,
                    )

                    with open(output_file, 'w', encoding='utf-8', newline='') as f:
                        for row in df.itertuples(index=False, name=None):
                            f.write('\t'.join(format_cell(cell) for cell in row) + '\n')
                    
                    print(f"成功转换 -> {output_file}")
                    count += 1
                
                except Exception as e:
                    print(f"处理 {file_path} 时出错: {e}")

    print(f"\n任务完成！共转换了 {count} 个文件。")

if __name__ == "__main__":
    convert_excel_to_txt('.')
    
    # --- 关键修改：在这里添加暂停 ---
    print("\n" + "="*30)
    input("按回车键(Enter)退出程序...")
