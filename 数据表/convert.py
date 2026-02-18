import pandas as pd
import os
import csv

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
    
    for file_name in os.listdir(scan_dir):
        # 跳过 Excel 打开时生成的临时锁文件
        if file_name.startswith('~$'):
            continue
        if file_name.endswith(('.xlsx', '.xls')):
            file_path = os.path.join(scan_dir, file_name)
            base_name = os.path.splitext(file_name)[0]
            output_file = os.path.join(target_dir, f"{base_name}.txt")
            
            print(f"正在处理: {file_name}...")
            
            try:
                # 读取 Excel
                df = pd.read_excel(file_path, header=None)
                
                # 预处理：将 NaN 替换为空字符串，否则导出会变成 "nan"
                df = df.fillna('')

                # 导出设置：
                # 1. sep='\t' : 使用制表符分隔
                # 2. quoting=csv.QUOTE_NONE : 不使用引号包裹字段，也不会把 " 变成 ""
                # 3. escapechar='\\' : 如果单元格内恰好有 Tab 键，会用反斜杠转义，防止数据列错位
                df.to_csv(
                    output_file, 
                    sep='\t', 
                    index=False, 
                    header=False, 
                    encoding='utf-8', 
                    quoting=csv.QUOTE_NONE, 
                    escapechar='\\' 
                )
                
                print(f"成功转换 -> {output_file}")
                count += 1
            
            except Exception as e:
                print(f"处理 {file_name} 时出错: {e}")

    print(f"\n任务完成！共转换了 {count} 个文件。")

if __name__ == "__main__":
    convert_excel_to_txt('.')
    
    # --- 关键修改：在这里添加暂停 ---
    print("\n" + "="*30)
    input("按回车键(Enter)退出程序...")
