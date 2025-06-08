import pandas as pd
import matplotlib.pyplot as plt
import matplotlib.dates as mdates
from datetime import datetime
import numpy as np
import re

def parse_markdown_table(file_path):
    """Parse markdown table from file and return DataFrame"""
    with open(file_path, 'r') as file:
        content = file.read()
    
    # Split content by lines and find table rows
    lines = content.strip().split('\n')
    
    # Find header and data rows (skip separator line with |---|)
    table_lines = []
    for line in lines:
        if line.strip().startswith('|') and '---' not in line:
            table_lines.append(line)
    
    if len(table_lines) < 2:
        raise ValueError("No valid markdown table found in file")
    
    # Parse header
    header = [col.strip() for col in table_lines[0].split('|')[1:-1]]
    
    # Parse data rows
    data = []
    for line in table_lines[1:]:
        row = [col.strip() for col in line.split('|')[1:-1]]
        data.append(row)
    
    # Create DataFrame
    df = pd.DataFrame(data, columns=header)
    
    # Convert data types
    df['Iteration'] = df['Iteration'].astype(int)
    df['Start Date'] = pd.to_datetime(df['Start Date'])
    df['End Date'] = pd.to_datetime(df['End Date'])
    #df['Duration'] = df['Duration'].astype(int)
    df['Duration'] = (df['End Date'] - df['Start Date']).dt.days
    
    return df

def save_html_table(df, output_file='table.html'):
    """Save the DataFrame as a styled HTML table with collapsed borders and minimal padding."""
    styles = [
        dict(selector="table", props=[
            ("border-collapse", "collapse"),
            ("width", "100%"),
            # ("border", "1px solid black")
        ]),
        dict(selector="th", props=[
            ("text-align", "center"),
            ("background-color", "#4CAF50"),
            ("color", "white"),
            ("font-size", "7px"),
            #("padding", "2px"),
            # ("border", "1px solid black"),
            # ("border-collapse", "collapse"),
        ]),
        dict(selector="td", props=[
            ("text-align", "left"),
            ("font-size", "7px"),
            #("padding", "2px"),
            # ("border", "1px solid black"),
            # ("border-collapse", "collapse"),
        ])
    ]
    html = (df.style
            .set_table_styles(styles)
            .set_properties(**{'text-align': 'left'})
            .hide(axis="index")
            .to_html())
    custom_css = """
    <style>
    body { font-family: Arial, sans-serif;
     font-size: 7px;
       }
    table, th, td {
    border: 1px solid black;
    border-collapse: collapse;
    }
    </style>
    """
    html = custom_css + html
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(html)
    print(f"HTML table saved as {output_file}")

def create_gantt_chart(df, output_file='data/gantt_chart.png'):
    """Create visualization with table and Gantt chart"""
    
    # Set up the figure with subplots
    fig = plt.figure(figsize=(14, 10))
    
   
    
    # Create Gantt chart subplot (bottom)
    #ax_gantt = plt.subplot(2, 1, 2)
    ax_gantt = fig.add_axes([0.07, 0.13, 0.88, 0.60]) 
    
    # Get unique iterations and create color map
    iterations = sorted(df['Iteration'].unique())
    colors = plt.cm.Set3(np.linspace(0, 1, len(iterations)))
    iteration_colors = dict(zip(iterations, colors))
    
    # Plot Gantt bars (reversed order - highest iteration first)
    y_pos = 0
    y_labels = []
    y_positions = []
    
    for iteration in reversed(iterations):
        iter_data = df[df['Iteration'] == iteration].sort_values('Start Date')
        
        # Add iteration separator
        if y_pos > 0:
            ax_gantt.axhline(y=y_pos - 0.5, color='gray', linestyle='--', alpha=0.5)
        
        for _, row in iter_data.iterrows():
            start_date = row['Start Date']
            end_date = row['End Date']
            duration = (end_date - start_date).days + 1
            
            # Create bar
            ax_gantt.barh(y_pos, duration, left=start_date, 
                         color=iteration_colors[iteration], 
                         alpha=0.7, height=0.6,
                         edgecolor='black', linewidth=0.5)
            
            # Create y-axis label
            status_indicator = "✓" if row['Status'].lower() == 'done' else "⚠"
            y_labels.append(f"Iter {iteration}: {row['Feature']} {status_indicator}")
            y_positions.append(y_pos)
            
            y_pos += 1
    
    # Customize Gantt chart
    ax_gantt.set_yticks(y_positions)
    ax_gantt.set_yticklabels(y_labels)

    ax_gantt.set_title('Project Gantt Chart', fontsize=16, fontweight='bold', pad=20)
    
    # Format x-axis dates
    ax_gantt.xaxis.set_major_formatter(mdates.DateFormatter('%m/%d/%Y'))
    ax_gantt.xaxis.set_major_locator(mdates.DayLocator(interval=7))
    plt.setp(ax_gantt.xaxis.get_majorticklabels(), rotation=45)
    
    # Add grid
    ax_gantt.grid(True, alpha=0.3, axis='x')
    

    
    # Adjust layout and save
    plt.subplots_adjust(top=0.80, bottom=0.05, left=0.05, right=0.98, hspace=0.15)  # Increase top, reduce hspace
    plt.savefig(output_file, dpi=300, bbox_inches='tight')
    #plt.show()
    
    print(f"Chart saved as {output_file}")


# Main execution
if __name__ == "__main__":
    try:
        # Parse the markdown table (expects schedule.md to exist)
        df = parse_markdown_table('data/schedule.md')
        print("Data loaded successfully:")
        print(df)
        print()
        save_html_table(df, 'data/schedule_table.html')
        
        # Generate the visualization
        create_gantt_chart(df, 'data/project_gantt_chart.png')
        
    except FileNotFoundError:
        print("Error: 'schedule.md' file not found.")
        print("Please create a schedule.md file with your table data.")
        print("\nExpected format:")
        print("| Iteration | Feature | Start Date | End Date | Duration | Status |")
        print("|-----------|---------|------------|----------|----------|---------|")
        print("| 1 | Feature 1 | 6/2/2025 | 6/10/2025 | 8 | Done |")
        print("| 1 | Feature 2 | 6/5/2025 | 6/12/2025 | 7 | In progress |")
    except Exception as e:
        print(f"Error: {e}")